import { describe, expect, it } from 'vitest';

import { mediaAnimationKeyframes } from './media-animation';

describe('mediaAnimationKeyframes', () => {
  it('builds a multi-stage orbital path that finishes at the resting position', () => {
    const frames = mediaAnimationKeyframes('orbit', true);

    expect(frames.length).toBeGreaterThan(3);
    expect(frames.at(-1)?.['transform']).toContain('translate(0, 0)');
  });

  it('interpolates shape properties for the morph transition', () => {
    const frames = mediaAnimationKeyframes('morph', true);

    expect(frames.some(frame => frame['clipPath'] !== undefined)).toBe(true);
    expect(frames.some(frame => frame['borderRadius'] === '50%')).toBe(true);
  });
});
