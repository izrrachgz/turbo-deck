import { DOCUMENT } from '@angular/common';
import { AfterViewInit, Component, ElementRef, OnDestroy, ViewChild, ViewEncapsulation, inject, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { PitchShift, Player, gainToDb, getContext, start as startAudio } from 'tone';

import { PlaybackQueueItemModel, PlaybackQueueSnapshotModel } from '../../../../core/backend-api.service';
import { TextPipe } from '../../../../core/i18n/text.pipe';
import { AudioPlaybackProgressView, createAudioPlaybackProgress } from '../../../../shared/media/audio-playback-progress';
import { VisualizerPlacementService } from '../../services/visualizer-placement.service';

interface RealtimeMessage {
  readonly name: string;
  readonly payload: unknown;
}

interface VisualizerState {
  readonly queue: PlaybackQueueSnapshotModel;
  readonly safeMargin: number;
}

@Component({
  selector: 'td-visualizer-page',
  standalone: true,
  imports: [TextPipe],
  templateUrl: './visualizer.page.html',
  styleUrl: './visualizer.page.css',
  encapsulation: ViewEncapsulation.None,
})
export class VisualizerPage implements AfterViewInit, OnDestroy {
  private static readonly playbackPermissionKey = 'turbo-deck.visualizer.playback-enabled';

  @ViewChild('stage', { static: true }) private stage!: ElementRef<HTMLElement>;

  private readonly document = inject(DOCUMENT);
  private readonly placement = inject(VisualizerPlacementService);
  private connection?: HubConnection;
  private current?: PlaybackQueueItemModel;
  private pending?: PlaybackQueueItemModel;
  private mediaElement?: HTMLElement;
  private player?: Player;
  private pitchShift?: PitchShift;
  private audioProgress?: AudioPlaybackProgressView;
  private completionTimer?: ReturnType<typeof setTimeout>;
  private statePollingTimer?: ReturnType<typeof setInterval>;
  private safeMargin = 24;
  private destroyed = false;
  private synchronizing = false;

  readonly error = signal('');
  readonly playbackEnabled = signal(false);
  readonly playbackActivationRequired = signal(false);
  readonly enablingPlayback = signal(false);

  constructor() {
    this.document.documentElement.classList.add('td-visualizer-root');
    this.document.body.classList.add('td-visualizer-body');
  }

  async ngAfterViewInit(): Promise<void> {
    if (this.hasRememberedPlaybackPermission()) this.playbackEnabled.set(true);
    const clientId = `visualizer-${crypto.randomUUID()}`;
    this.connection = new HubConnectionBuilder()
      .withUrl(`/hubs/visualizer?clientId=${encodeURIComponent(clientId)}`)
      .withAutomaticReconnect([0, 1000, 5000, 10000])
      .configureLogging(LogLevel.Warning)
      .build();
    this.connection.on('Receive', (message: RealtimeMessage) => {
      void this.receive(message).catch(error => this.error.set(this.message(error)));
    });
    this.connection.onreconnected(() => {
      void this.synchronize().catch(error => this.error.set(this.message(error)));
    });

    void this.connection.start().catch(error => this.error.set(this.message(error)));
    try {
      await this.synchronize();
    } catch (error) {
      this.error.set(this.message(error));
    }
    this.startStatePolling();
  }

  ngOnDestroy(): void {
    this.destroyed = true;
    this.pending = undefined;
    if (this.statePollingTimer) clearInterval(this.statePollingTimer);
    this.statePollingTimer = undefined;
    this.cleanupPlayback();
    void this.connection?.stop();
    this.document.documentElement.classList.remove('td-visualizer-root');
    this.document.body.classList.remove('td-visualizer-body');
  }

  async enablePlayback(): Promise<void> {
    this.enablingPlayback.set(true);
    this.error.set('');
    try {
      const htmlMediaUnlock = this.unlockHtmlMedia();
      const webAudioUnlock = this.resumeAudioContext(10000);
      await Promise.all([htmlMediaUnlock, webAudioUnlock]);
      await this.markPlaybackEnabled();
    } catch (error) {
      this.forgetPlaybackPermission();
      this.error.set(this.message(error));
    } finally {
      this.enablingPlayback.set(false);
    }
  }

  private async resumeAudioContext(timeoutMilliseconds?: number): Promise<void> {
    let timeout: ReturnType<typeof setTimeout> | undefined;
    const attempts: Promise<unknown>[] = [startAudio()];
    if (timeoutMilliseconds) {
      attempts.push(new Promise((_, reject) => {
        timeout = setTimeout(() => reject(new Error('Autoplay permission is required.')), timeoutMilliseconds);
      }));
    }

    try {
      await Promise.race(attempts);
    } finally {
      if (timeout) clearTimeout(timeout);
    }
    if (getContext().state !== 'running') throw new Error('The browser did not enable media playback.');
  }

  private async markPlaybackEnabled(): Promise<void> {
    this.rememberPlaybackPermission();
    this.playbackEnabled.set(true);
    this.playbackActivationRequired.set(false);
    const pending = this.pending;
    this.pending = undefined;
    if (pending) await this.play(pending);
  }

  private async receive(message: RealtimeMessage): Promise<void> {
    if (message.name === 'playback.requested' || message.name === 'playback.queue.changed') {
      await this.synchronize();
    } else if (message.name === 'playback.stop.requested') {
      this.pending = undefined;
      this.cleanupPlayback();
    }
  }

  private async synchronize(): Promise<void> {
    if (this.synchronizing || this.destroyed) return;
    this.synchronizing = true;
    try {
      const response = await fetch('/api/v1/visualizer/state', {
        cache: 'no-store',
      });
      if (!response.ok) throw new Error(`Visualizer state request failed (${response.status}).`);
      const state = await response.json() as VisualizerState;
      this.safeMargin = state.safeMargin;
      await this.reconcile(state.queue);
      this.error.set('');
    } finally {
      this.synchronizing = false;
    }
  }

  private startStatePolling(): void {
    if (this.statePollingTimer || this.destroyed) return;
    this.statePollingTimer = setInterval(() => {
      void this.synchronize().catch(error => this.error.set(this.message(error)));
    }, 1000);
  }

  private async reconcile(queue: PlaybackQueueSnapshotModel): Promise<void> {
    if (!queue.current) {
      this.pending = undefined;
      this.cleanupPlayback();
      return;
    }
    if (this.current?.id === queue.current.id || this.pending?.id === queue.current.id) return;
    await this.play(queue.current);
  }

  private async play(item: PlaybackQueueItemModel): Promise<void> {
    if (this.destroyed || this.current?.id === item.id) return;
    if (item.kind !== 'image' && !this.playbackEnabled()) {
      this.pending = item;
      this.playbackActivationRequired.set(true);
      return;
    }
    if (item.kind === 'image') this.playbackActivationRequired.set(false);
    this.pending = undefined;
    this.cleanupPlayback();
    this.current = item;
    this.error.set('');
    const mediaUrl = `/api/v1/visualizer/media/${item.mediaAssetId}`;

    try {
      if (item.kind === 'image') this.playImage(item, mediaUrl);
      else if (item.kind === 'video') this.playVideo(item, mediaUrl);
      else if (item.kind === 'audio') await this.playAudio(item, mediaUrl);
      else await this.complete(item.id, false, 'unsupported_media_kind');
    } catch (error) {
      if (item.kind === 'audio' && getContext().state !== 'running') {
        this.deferForPlaybackActivation(item);
        return;
      }
      this.error.set(this.message(error));
      await this.complete(item.id, false, 'playback_start_failed');
    }
  }

  private playImage(item: PlaybackQueueItemModel, mediaUrl: string): void {
    const image = new Image();
    image.className = 'td-visualizer-media';
    image.alt = '';
    image.onload = () => {
      this.position(image, item, image.naturalWidth, image.naturalHeight);
      image.classList.add('td-visualizer-media-visible');
      void this.animateMedia(image, item.animationInType, item.animationInDurationMilliseconds, true);
      if (!item.displayPermanently) {
        this.completionTimer = setTimeout(() => void this.finishVisual(item, image), item.displayDurationMilliseconds);
      }
    };
    image.onerror = () => void this.complete(item.id, false, 'image_load_failed');
    image.src = mediaUrl;
    this.mediaElement = image;
    this.stage.nativeElement.append(image);
  }

  private playVideo(item: PlaybackQueueItemModel, mediaUrl: string): void {
    const video = this.document.createElement('video');
    video.className = 'td-visualizer-media';
    video.autoplay = true;
    video.playsInline = true;
    video.preload = 'auto';
    video.volume = item.volume;
    video.playbackRate = item.playbackRate;
    video.preservesPitch = false;
    video.onloadedmetadata = () => {
      this.position(video, item, video.videoWidth, video.videoHeight);
      video.classList.add('td-visualizer-media-visible');
      void this.animateMedia(video, item.animationInType, item.animationInDurationMilliseconds, true);
      void video.play().catch(error => {
        if (error instanceof DOMException && error.name === 'NotAllowedError') {
          this.deferForPlaybackActivation(item);
          return;
        }
        void this.complete(item.id, false, 'video_playback_failed');
      });
    };
    video.onended = () => void this.finishVisual(item, video);
    video.onerror = () => void this.complete(item.id, false, 'video_load_failed');
    video.src = mediaUrl;
    this.mediaElement = video;
    this.stage.nativeElement.append(video);
  }

  private async playAudio(item: PlaybackQueueItemModel, mediaUrl: string): Promise<void> {
    await this.resumeAudioContext(5000);
    const ratePitchOffset = 12 * Math.log2(item.playbackRate);
    const pitchShift = new PitchShift({ pitch: item.pitchSemitones - ratePitchOffset }).toDestination();
    const player = new Player().connect(pitchShift);
    player.volume.value = item.volume <= 0 ? -Infinity : gainToDb(item.volume);
    player.playbackRate = item.playbackRate;
    await player.load(mediaUrl);
    if (this.current?.id !== item.id) {
      player.dispose();
      pitchShift.dispose();
      return;
    }

    const sourceDuration = item.sourceDurationMilliseconds ?? Math.round(player.buffer.duration * 1000);
    const playbackDuration = Math.max(250, Math.round(sourceDuration / item.playbackRate));
    const badge = this.createAudioCard(item, player, sourceDuration);
    this.positionAudio(badge);
    this.mediaElement = badge;
    this.player = player;
    this.pitchShift = pitchShift;
    this.stage.nativeElement.append(badge);
    player.start();
    this.audioProgress?.start(item.playbackRate);
    this.completionTimer = setTimeout(() => void this.complete(item.id), playbackDuration + 100);
  }

  private createAudioCard(item: PlaybackQueueItemModel, player: Player, sourceDurationMilliseconds: number): HTMLElement {
    const card = this.document.createElement('article');
    card.className = 'td-visualizer-audio td-visualizer-media-visible';

    const icon = this.document.createElement('span');
    icon.className = 'td-visualizer-audio-icon';
    icon.setAttribute('aria-hidden', 'true');

    const content = this.document.createElement('div');
    content.className = 'td-visualizer-audio-content';
    const heading = this.document.createElement('div');
    heading.className = 'td-visualizer-audio-heading';
    const name = this.document.createElement('strong');
    name.textContent = item.label;
    heading.append(name);
    if (item.playbackRate > 1.001) {
      const speed = this.document.createElement('span');
      speed.className = 'td-visualizer-audio-speed';
      speed.textContent = `${this.formatRate(item.playbackRate)}x`;
      heading.append(speed);
    }

    const progress = createAudioPlaybackProgress(
      this.document,
      this.waveformPeaks(item.waveformData, player, 48),
      sourceDurationMilliseconds,
    );
    progress.time.className = 'td-visualizer-audio-duration';
    this.audioProgress = progress;
    content.append(heading, progress.waveform);
    card.append(icon, content, progress.time);
    return card;
  }

  private waveformPeaks(serialized: string | null | undefined, player: Player, bucketCount: number): number[] {
    try {
      const samples = player.buffer.getChannelData(0);
      if (samples.length) return this.normalizePeaks(this.rmsPeaks(samples, bucketCount));
    } catch {
      // Fall back to persisted inspection metadata if the decoded buffer is unavailable.
    }

    if (serialized) {
      try {
        const values = JSON.parse(serialized) as unknown;
        if (Array.isArray(values)) {
          const numeric = values.map(value => Number(value)).filter(value => Number.isFinite(value));
          if (numeric.length) return this.normalizePeaks(this.resamplePeaks(numeric, bucketCount));
        }
      } catch {
        // Use a quiet neutral waveform for malformed legacy metadata.
      }
    }

    return Array.from({ length: bucketCount }, () => 0.08);
  }

  private rmsPeaks(samples: Float32Array, bucketCount: number): number[] {
    const peaks: number[] = [];
    for (let bucket = 0; bucket < bucketCount; bucket += 1) {
      const start = Math.floor(bucket * samples.length / bucketCount);
      const end = Math.max(start + 1, Math.floor((bucket + 1) * samples.length / bucketCount));
      const stride = Math.max(1, Math.floor((end - start) / 2048));
      let squareSum = 0;
      let count = 0;
      for (let index = start; index < end; index += stride) {
        const sample = samples[index] ?? 0;
        squareSum += sample * sample;
        count += 1;
      }
      peaks.push(count ? Math.sqrt(squareSum / count) : 0);
    }
    return peaks;
  }

  private normalizePeaks(values: number[]): number[] {
    const magnitudes = values.map(value => Math.max(0, Math.abs(value)));
    const maximum = Math.max(...magnitudes, 0);
    if (maximum < 0.00001) return magnitudes.map(() => 0.08);
    return magnitudes.map(value => {
      const relative = Math.min(1, value / maximum);
      return relative < 0.01 ? 0.08 : 0.12 + Math.pow(relative, 0.65) * 0.88;
    });
  }

  private resamplePeaks(values: number[], bucketCount: number): number[] {
    return Array.from({ length: bucketCount }, (_, bucket) => {
      const start = Math.floor(bucket * values.length / bucketCount);
      const end = Math.max(start + 1, Math.floor((bucket + 1) * values.length / bucketCount));
      return Math.max(...values.slice(start, end));
    });
  }

  private positionAudio(element: HTMLElement): void {
    const container = this.stageSize();
    const rectangle = this.placement.placeFixed(container.width, container.height, 440, 96, this.safeMargin);
    Object.assign(element.style, {
      position: 'absolute',
      left: `${rectangle.left}px`,
      top: `${rectangle.top}px`,
      width: `${rectangle.width}px`,
      height: `${rectangle.height}px`,
    });
  }

  private formatRate(rate: number): string {
    return Number(rate.toFixed(2)).toString();
  }

  private deferForPlaybackActivation(item: PlaybackQueueItemModel): void {
    if (this.current?.id !== item.id) return;
    this.cleanupPlayback();
    this.pending = item;
    this.forgetPlaybackPermission();
    this.playbackEnabled.set(false);
    this.playbackActivationRequired.set(true);
  }

  private async unlockHtmlMedia(timeoutMilliseconds = 10000): Promise<void> {
    const audio = this.document.createElement('audio');
    const objectUrl = URL.createObjectURL(this.createSilentWave());
    audio.preload = 'auto';
    audio.src = objectUrl;
    audio.volume = 0.01;
    audio.setAttribute('aria-hidden', 'true');
    audio.style.display = 'none';
    this.document.body.append(audio);

    let timeout: ReturnType<typeof setTimeout> | undefined;
    try {
      await Promise.race([
        audio.play(),
        new Promise((_, reject) => {
          timeout = setTimeout(() => reject(new Error('HTML media playback permission timed out.')), timeoutMilliseconds);
        }),
      ]);
    } finally {
      if (timeout) clearTimeout(timeout);
      audio.pause();
      audio.removeAttribute('src');
      audio.load();
      audio.remove();
      URL.revokeObjectURL(objectUrl);
    }
  }

  private createSilentWave(): Blob {
    const sampleCount = 400;
    const buffer = new ArrayBuffer(44 + sampleCount);
    const view = new DataView(buffer);
    const writeText = (offset: number, value: string) => {
      for (let index = 0; index < value.length; index += 1) view.setUint8(offset + index, value.charCodeAt(index));
    };
    writeText(0, 'RIFF');
    view.setUint32(4, 36 + sampleCount, true);
    writeText(8, 'WAVE');
    writeText(12, 'fmt ');
    view.setUint32(16, 16, true);
    view.setUint16(20, 1, true);
    view.setUint16(22, 1, true);
    view.setUint32(24, 8000, true);
    view.setUint32(28, 8000, true);
    view.setUint16(32, 1, true);
    view.setUint16(34, 8, true);
    writeText(36, 'data');
    view.setUint32(40, sampleCount, true);
    new Uint8Array(buffer, 44).fill(128);
    return new Blob([buffer], { type: 'audio/wav' });
  }

  private hasRememberedPlaybackPermission(): boolean {
    try { return localStorage.getItem(VisualizerPage.playbackPermissionKey) === 'true'; }
    catch { return false; }
  }

  private rememberPlaybackPermission(): void {
    try { localStorage.setItem(VisualizerPage.playbackPermissionKey, 'true'); }
    catch { /* Playback remains available for this page even if storage is unavailable. */ }
  }

  private forgetPlaybackPermission(): void {
    try { localStorage.removeItem(VisualizerPage.playbackPermissionKey); }
    catch { /* Storage may be unavailable in a restricted browser source. */ }
  }

  private position(element: HTMLElement, item: PlaybackQueueItemModel, naturalWidth: number, naturalHeight: number): void {
    const container = this.stageSize();
    const rectangle = this.placement.place(
      container.width,
      container.height,
      Math.max(1, naturalWidth || item.width || 640),
      Math.max(1, naturalHeight || item.height || 360),
      this.safeMargin,
      item.displaySize,
    );
    Object.assign(element.style, {
      position: 'absolute',
      left: `${rectangle.left}px`,
      top: `${rectangle.top}px`,
      width: `${rectangle.width}px`,
      height: `${rectangle.height}px`,
    });
  }

  private stageSize(): { readonly width: number; readonly height: number } {
    const stage = this.stage.nativeElement;
    const rectangle = stage.getBoundingClientRect();
    return {
      width: Math.max(1, Math.floor(rectangle.width || stage.clientWidth || window.innerWidth)),
      height: Math.max(1, Math.floor(rectangle.height || stage.clientHeight || window.innerHeight)),
    };
  }

  private async finishVisual(item: PlaybackQueueItemModel, element: HTMLElement): Promise<void> {
    if (this.current?.id !== item.id || this.mediaElement !== element) return;
    await this.animateMedia(element, item.animationOutType, item.animationOutDurationMilliseconds, false);
    await this.complete(item.id);
  }

  private async animateMedia(
    element: HTMLElement,
    type: PlaybackQueueItemModel['animationInType'],
    durationMilliseconds: number,
    entering: boolean,
  ): Promise<void> {
    if (type === 'none' || durationMilliseconds <= 0 || window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;
    const visible = { opacity: 1, transform: 'translate(0) scale(1)' };
    const hidden = type === 'slide'
      ? { opacity: 0, transform: entering ? 'translateY(18%) scale(1)' : 'translateY(-18%) scale(1)' }
      : type === 'zoom'
        ? { opacity: 0, transform: entering ? 'translate(0) scale(.72)' : 'translate(0) scale(1.18)' }
        : { opacity: 0, transform: 'translate(0) scale(1)' };
    try {
      await element.animate(entering ? [hidden, visible] : [visible, hidden], {
        duration: durationMilliseconds,
        easing: 'ease-in-out',
        fill: 'forwards',
      }).finished;
    } catch {
      // A queue override or clear can remove media while its transition is active.
    }
  }

  private async complete(itemId: string, success = true, errorCode?: string): Promise<void> {
    if (this.current?.id !== itemId) return;
    this.cleanupPlayback();
    try {
      const response = await fetch('/api/v1/visualizer/complete', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ itemId, success, errorCode }),
      });
      if (!response.ok) throw new Error(`Playback completion failed (${response.status}).`);
      await this.reconcile(await response.json() as PlaybackQueueSnapshotModel);
    } catch (error) {
      this.error.set(this.message(error));
    }
  }

  private cleanupPlayback(): void {
    if (this.completionTimer) clearTimeout(this.completionTimer);
    this.completionTimer = undefined;
    this.audioProgress?.stop();
    this.audioProgress = undefined;
    if (this.mediaElement instanceof HTMLMediaElement) {
      this.mediaElement.pause();
      this.mediaElement.removeAttribute('src');
      this.mediaElement.load();
    }
    this.mediaElement?.remove();
    this.mediaElement = undefined;
    this.player?.stop();
    this.player?.dispose();
    this.pitchShift?.dispose();
    this.player = undefined;
    this.pitchShift = undefined;
    this.current = undefined;
  }

  private message(error: unknown): string {
    return error instanceof Error ? error.message : String(error);
  }
}
