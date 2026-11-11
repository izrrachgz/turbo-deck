import { Injectable } from '@angular/core';

export interface VisualizerRectangle {
  readonly left: number;
  readonly top: number;
  readonly width: number;
  readonly height: number;
}

@Injectable({ providedIn: 'root' })
export class VisualizerPlacementService {
  place(
    containerWidth: number,
    containerHeight: number,
    naturalWidth: number,
    naturalHeight: number,
    safeMargin: number,
    displaySize: 'small' | 'medium' | 'large' | 'fullscreen' = 'medium',
    random: () => number = Math.random,
  ): VisualizerRectangle {
    const bounds = this.bounds(containerWidth, containerHeight, safeMargin);
    if (displaySize === 'fullscreen') {
      return { left: bounds.margin, top: bounds.margin, width: bounds.width, height: bounds.height };
    }

    const sizeRatio = displaySize === 'small' ? 0.3 : displaySize === 'large' ? 0.8 : 0.55;
    const sourceWidth = Math.max(1, naturalWidth);
    const sourceHeight = Math.max(1, naturalHeight);
    const scale = Math.min(bounds.width * sizeRatio / sourceWidth, bounds.height * sizeRatio / sourceHeight);
    return this.placeFixed(
      containerWidth,
      containerHeight,
      Math.max(1, Math.round(sourceWidth * scale)),
      Math.max(1, Math.round(sourceHeight * scale)),
      safeMargin,
      random,
    );
  }

  placeFixed(
    containerWidth: number,
    containerHeight: number,
    requestedWidth: number,
    requestedHeight: number,
    safeMargin: number,
    random: () => number = Math.random,
  ): VisualizerRectangle {
    const bounds = this.bounds(containerWidth, containerHeight, safeMargin);
    const width = Math.max(1, Math.min(Math.round(requestedWidth), bounds.width));
    const height = Math.max(1, Math.min(Math.round(requestedHeight), bounds.height));
    const horizontalTravel = Math.max(0, bounds.width - width);
    const verticalTravel = Math.max(0, bounds.height - height);
    const left = bounds.margin + Math.round(horizontalTravel * this.randomValue(random));
    const top = bounds.margin + Math.round(verticalTravel * this.randomValue(random));

    return { left, top, width, height };
  }

  private bounds(containerWidth: number, containerHeight: number, safeMargin: number): {
    readonly margin: number;
    readonly width: number;
    readonly height: number;
  } {
    const width = Math.max(1, Math.floor(containerWidth));
    const height = Math.max(1, Math.floor(containerHeight));
    const margin = Math.max(0, Math.min(safeMargin, Math.floor((Math.min(width, height) - 1) / 2)));
    return {
      margin,
      width: Math.max(1, width - margin * 2),
      height: Math.max(1, height - margin * 2),
    };
  }

  private randomValue(random: () => number): number {
    const value = random();
    if (!Number.isFinite(value)) return 0;
    return Math.max(0, Math.min(1, value));
  }
}
