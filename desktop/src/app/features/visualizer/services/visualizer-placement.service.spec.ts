import { describe, expect, it } from 'vitest';

import { VisualizerPlacementService } from './visualizer-placement.service';

describe('VisualizerPlacementService', () => {
  const service = new VisualizerPlacementService();

  it('keeps the complete media rectangle inside the container safe bounds', () => {
    const rectangle = service.place(800, 600, 1600, 900, 24, 'large', () => 1);

    expect(rectangle.left).toBeGreaterThanOrEqual(24);
    expect(rectangle.top).toBeGreaterThanOrEqual(24);
    expect(rectangle.left + rectangle.width).toBeLessThanOrEqual(776);
    expect(rectangle.top + rectangle.height).toBeLessThanOrEqual(576);
  });

  it('uses continuous random coordinates instead of fixed screen anchors', () => {
    const first = service.place(1000, 700, 640, 360, 20, 'medium', () => 0.21);
    const second = service.place(1000, 700, 640, 360, 20, 'medium', () => 0.73);

    expect(second.left).not.toBe(first.left);
    expect(second.top).not.toBe(first.top);
  });

  it('allows media rectangles to overlap', () => {
    const first = service.place(1280, 720, 640, 360, 24, 'medium', () => 0.5);
    const second = service.place(1280, 720, 800, 600, 24, 'medium', () => 0.5);
    const overlaps = first.left < second.left + second.width && first.left + first.width > second.left &&
      first.top < second.top + second.height && first.top + first.height > second.top;

    expect(overlaps).toBe(true);
  });

  it('clamps fixed-size cards to small containers before positioning them', () => {
    const rectangle = service.placeFixed(320, 180, 440, 96, 24, () => 1);

    expect(rectangle).toEqual({ left: 24, top: 60, width: 272, height: 96 });
    expect(rectangle.left + rectangle.width).toBeLessThanOrEqual(296);
    expect(rectangle.top + rectangle.height).toBeLessThanOrEqual(156);
  });

  it('uses the full safe container for full-screen media', () => {
    const rectangle = service.place(1920, 1080, 1600, 900, 48, 'fullscreen');

    expect(rectangle).toEqual({ left: 48, top: 48, width: 1824, height: 984 });
  });
});
