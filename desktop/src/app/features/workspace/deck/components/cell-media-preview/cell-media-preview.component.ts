import { Component, ElementRef, Input, OnDestroy, ViewChild, signal } from '@angular/core';
import { PitchShift, Player, gainToDb, start as startAudio } from 'tone';

import { MediaAssetModel } from '../../../../../core/backend-api.service';
import { TextPipe } from '../../../../../core/i18n/text.pipe';

export type CellDisplaySize = 'small' | 'medium' | 'large' | 'fullscreen';
export type CellAnimationType = 'none' | 'fade' | 'slide' | 'zoom';

@Component({
  selector: 'td-cell-media-preview',
  standalone: true,
  imports: [TextPipe],
  templateUrl: './cell-media-preview.component.html',
  styleUrl: './cell-media-preview.component.css',
})
export class CellMediaPreviewComponent implements OnDestroy {
  @ViewChild('stage', { static: true }) private stage!: ElementRef<HTMLElement>;
  @Input() media?: MediaAssetModel;
  @Input() label = '';
  @Input() playbackRate = 1;
  @Input() pitchSemitones = 0;
  @Input() displayDurationMilliseconds = 5000;
  @Input() displayPermanently = false;
  @Input() displaySize: CellDisplaySize = 'medium';
  @Input() animationInType: CellAnimationType = 'none';
  @Input() animationInDurationMilliseconds = 0;
  @Input() animationOutType: CellAnimationType = 'none';
  @Input() animationOutDurationMilliseconds = 0;

  private mediaElement?: HTMLElement;
  private player?: Player;
  private pitchShift?: PitchShift;
  private completionTimer?: ReturnType<typeof setTimeout>;

  readonly playing = signal(false);
  readonly error = signal('');

  async test(): Promise<void> {
    this.stop();
    if (!this.media || this.media.kind === 'unknown') {
      this.error.set('Select a supported media file first.');
      return;
    }

    this.error.set('');
    this.playing.set(true);
    const mediaUrl = `turbo-deck-media://content/${this.media.id}`;
    try {
      if (this.media.kind === 'image') this.previewImage(mediaUrl);
      else if (this.media.kind === 'video') this.previewVideo(mediaUrl);
      else await this.previewAudio(mediaUrl);
    } catch (error) {
      this.error.set(this.message(error));
      this.stopPlayback();
    }
  }

  stop(): void {
    this.error.set('');
    this.stopPlayback();
  }

  ngOnDestroy(): void {
    this.stopPlayback();
  }

  private previewImage(mediaUrl: string): void {
    const image = new Image();
    image.alt = '';
    image.className = this.mediaClassName();
    image.onload = () => {
      void this.animate(image, this.animationInType, this.animationInDurationMilliseconds, true);
      if (!this.displayPermanently) {
        this.completionTimer = setTimeout(() => void this.finishVisual(image), this.displayDurationMilliseconds);
      }
    };
    image.onerror = () => this.fail('The image preview could not be loaded.');
    image.src = mediaUrl;
    this.mediaElement = image;
    this.stage.nativeElement.append(image);
  }

  private previewVideo(mediaUrl: string): void {
    const video = document.createElement('video');
    video.className = this.mediaClassName();
    video.controls = true;
    video.preload = 'auto';
    video.onloadedmetadata = () => {
      void this.animate(video, this.animationInType, this.animationInDurationMilliseconds, true);
      void video.play().catch(error => this.fail(this.message(error)));
    };
    video.onended = () => void this.finishVisual(video);
    video.onerror = () => this.fail('The video preview could not be loaded.');
    video.src = mediaUrl;
    this.mediaElement = video;
    this.stage.nativeElement.append(video);
  }

  private async previewAudio(mediaUrl: string): Promise<void> {
    await startAudio();
    const ratePitchOffset = 12 * Math.log2(this.playbackRate);
    const pitchShift = new PitchShift({ pitch: this.pitchSemitones - ratePitchOffset }).toDestination();
    const player = new Player().connect(pitchShift);
    player.volume.value = gainToDb(1);
    player.playbackRate = this.playbackRate;
    this.player = player;
    this.pitchShift = pitchShift;
    await player.load(mediaUrl);

    const sourceDuration = this.media?.durationMilliseconds ?? Math.round(player.buffer.duration * 1000);
    const card = this.createAudioCard(player, sourceDuration);
    this.mediaElement = card;
    this.stage.nativeElement.append(card);
    player.start();
    this.completionTimer = setTimeout(() => this.stopPlayback(), Math.max(250, sourceDuration / this.playbackRate) + 100);
  }

  private createAudioCard(player: Player, sourceDurationMilliseconds: number): HTMLElement {
    const card = document.createElement('article');
    card.className = 'td-cell-preview-audio';

    const icon = document.createElement('span');
    icon.className = 'td-cell-preview-audio-icon';
    icon.setAttribute('aria-hidden', 'true');

    const content = document.createElement('div');
    content.className = 'td-cell-preview-audio-content';
    const heading = document.createElement('div');
    heading.className = 'td-cell-preview-audio-heading';
    const name = document.createElement('strong');
    name.textContent = this.label || this.media?.displayName || 'Audio';
    heading.append(name);
    if (this.playbackRate > 1.001) {
      const speed = document.createElement('span');
      speed.className = 'td-cell-preview-audio-speed';
      speed.textContent = `${this.formatRate(this.playbackRate)}x`;
      heading.append(speed);
    }

    const waveform = document.createElement('div');
    waveform.className = 'td-cell-preview-waveform';
    waveform.setAttribute('aria-hidden', 'true');
    for (const peak of this.waveformPeaks(this.media?.waveformData, player, 40)) {
      const bar = document.createElement('span');
      bar.style.height = `${Math.max(12, Math.round(peak * 100))}%`;
      waveform.append(bar);
    }

    const duration = document.createElement('time');
    duration.className = 'td-cell-preview-audio-duration';
    duration.textContent = this.formatDuration(sourceDurationMilliseconds);
    content.append(heading, waveform);
    card.append(icon, content, duration);
    return card;
  }

  private waveformPeaks(serialized: string | null | undefined, player: Player, bucketCount: number): number[] {
    try {
      const samples = player.buffer.getChannelData(0);
      if (samples.length) return this.normalizePeaks(this.rmsPeaks(samples, bucketCount));
    } catch {
      // Fall back to persisted media inspection metadata when the decoded buffer is unavailable.
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

  private formatDuration(milliseconds: number): string {
    const seconds = Math.max(0, Math.round(milliseconds / 1000));
    return `${Math.floor(seconds / 60)}:${String(seconds % 60).padStart(2, '0')}`;
  }

  private formatRate(rate: number): string {
    return Number(rate.toFixed(2)).toString();
  }

  private async finishVisual(element: HTMLElement): Promise<void> {
    if (this.mediaElement !== element) return;
    await this.animate(element, this.animationOutType, this.animationOutDurationMilliseconds, false);
    if (this.mediaElement === element) this.stopPlayback();
  }

  private async animate(
    element: HTMLElement,
    type: CellAnimationType,
    durationMilliseconds: number,
    entering: boolean,
  ): Promise<void> {
    element.style.opacity = '1';
    if (type === 'none' || durationMilliseconds <= 0) return;
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
      // Replacing or closing a preview cancels its animation.
    }
  }

  private mediaClassName(): string {
    return `td-cell-preview-media td-cell-preview-size-${this.displaySize}`;
  }

  private fail(message: string): void {
    this.stopPlayback();
    this.error.set(message);
  }

  private stopPlayback(): void {
    if (this.completionTimer) clearTimeout(this.completionTimer);
    this.completionTimer = undefined;
    if (this.mediaElement instanceof HTMLMediaElement) {
      this.mediaElement.pause();
      this.mediaElement.removeAttribute('src');
      this.mediaElement.load();
    }
    this.mediaElement?.remove();
    this.mediaElement = undefined;
    try { this.player?.stop(); } catch { /* The player may not have started yet. */ }
    this.player?.dispose();
    this.pitchShift?.dispose();
    this.player = undefined;
    this.pitchShift = undefined;
    this.playing.set(false);
  }

  private message(error: unknown): string {
    return error instanceof Error ? error.message : String(error);
  }
}
