export interface AudioPlaybackProgressView {
  readonly waveform: HTMLDivElement;
  readonly time: HTMLTimeElement;
  start(playbackRate: number): void;
  stop(): void;
}

export interface AudioProgressSnapshot {
  readonly elapsedMilliseconds: number;
  readonly percent: number;
}

export function audioProgressSnapshot(
  realElapsedMilliseconds: number,
  sourceDurationMilliseconds: number,
  playbackRate: number,
): AudioProgressSnapshot {
  const duration = Math.max(0, sourceDurationMilliseconds);
  const elapsed = Math.min(duration, Math.max(0, realElapsedMilliseconds) * Math.max(0.01, playbackRate));
  return {
    elapsedMilliseconds: elapsed,
    percent: duration > 0 ? elapsed / duration * 100 : 100,
  };
}

export function createAudioPlaybackProgress(
  document: Document,
  peaks: readonly number[],
  sourceDurationMilliseconds: number,
): AudioPlaybackProgressView {
  const duration = Math.max(0, sourceDurationMilliseconds);
  const waveform = document.createElement('div');
  waveform.className = 'td-audio-waveform';
  waveform.setAttribute('aria-hidden', 'true');

  const track = createBars(document, peaks, 'td-audio-waveform-bars td-audio-waveform-track');
  const played = createBars(document, peaks, 'td-audio-waveform-bars td-audio-waveform-played');
  played.style.clipPath = 'inset(0 100% 0 0)';
  waveform.append(track, played);

  const time = document.createElement('time');
  const totalText = formatAudioTime(duration, true);
  time.textContent = `0:00 / ${totalText}`;
  time.dateTime = 'PT0S';

  let animationFrame: number | undefined;
  let startedAt = 0;

  const stop = (): void => {
    if (animationFrame !== undefined) cancelAnimationFrame(animationFrame);
    animationFrame = undefined;
  };

  const start = (playbackRate: number): void => {
    stop();
    startedAt = performance.now();
    const render = (now: number): void => {
      const snapshot = audioProgressSnapshot(now - startedAt, duration, playbackRate);
      const remaining = Math.max(0, 100 - snapshot.percent);
      played.style.clipPath = `inset(0 ${remaining}% 0 0)`;
      time.textContent = `${formatAudioTime(snapshot.elapsedMilliseconds)} / ${totalText}`;
      time.dateTime = `PT${Math.floor(snapshot.elapsedMilliseconds / 1000)}S`;
      animationFrame = snapshot.percent < 100 ? requestAnimationFrame(render) : undefined;
    };
    render(startedAt);
  };

  return { waveform, time, start, stop };
}

export function formatAudioTime(milliseconds: number, roundUp = false): string {
  const seconds = Math.max(0, (roundUp ? Math.ceil : Math.floor)(milliseconds / 1000));
  return `${Math.floor(seconds / 60)}:${String(seconds % 60).padStart(2, '0')}`;
}

function createBars(document: Document, peaks: readonly number[], className: string): HTMLDivElement {
  const bars = document.createElement('div');
  bars.className = className;
  for (const peak of peaks) {
    const bar = document.createElement('span');
    bar.style.height = `${Math.max(12, Math.round(peak * 100))}%`;
    bars.append(bar);
  }
  return bars;
}
