import { CellAnimationType } from '../../core/backend-api.service';

const visible: Keyframe = { opacity: 1, transform: 'translate(0, 0) rotate(0deg) scale(1)' };

export function mediaAnimationKeyframes(type: CellAnimationType, entering: boolean): Keyframe[] {
  const entrance = entranceKeyframes(type);
  if (entering) return entrance;

  switch (type) {
    case 'slide':
      return [visible, { opacity: 0, transform: 'translateY(-28%) rotate(0deg) scale(.96)' }];
    case 'zoom':
      return [visible, { opacity: 0, transform: 'translate(0, 0) rotate(0deg) scale(1.32)' }];
    case 'spin':
      return [visible, { opacity: 0, transform: 'translate(0, 0) rotate(420deg) scale(.16)' }];
    case 'flip':
      return [
        { ...visible, transform: 'perspective(700px) rotateY(0deg) scale(1)' },
        { opacity: 0, transform: 'perspective(700px) rotateY(96deg) scale(.82)' },
      ];
    case 'bounce':
      return [
        visible,
        { offset: .2, opacity: 1, transform: 'translateY(-12%) rotate(0deg) scale(1.04)' },
        { opacity: 0, transform: 'translateY(72%) rotate(0deg) scale(.72)' },
      ];
    case 'morph':
      return [
        { ...visible, borderRadius: '0%', clipPath: 'inset(0% round 0%)' },
        { offset: .48, opacity: 1, transform: 'translate(0, 0) rotate(12deg) scale(.92)', borderRadius: '42% 58% 65% 35% / 38% 35% 65% 62%', clipPath: 'polygon(50% 0%, 96% 28%, 82% 88%, 20% 96%, 2% 38%)' },
        { opacity: 0, transform: 'translate(0, 0) rotate(42deg) scale(.2)', borderRadius: '50%', clipPath: 'circle(8% at 50% 50%)' },
      ];
    case 'orbit':
      return [
        visible,
        { offset: .2, opacity: 1, transform: 'translate(38%, -28%) rotate(90deg) scale(.9)' },
        { offset: .45, opacity: .9, transform: 'translate(54%, 34%) rotate(190deg) scale(.72)' },
        { offset: .72, opacity: .65, transform: 'translate(-32%, 44%) rotate(280deg) scale(.5)' },
        { opacity: 0, transform: 'translate(-58%, -24%) rotate(420deg) scale(.18)' },
      ];
    case 'blur':
      return [visible, { opacity: 0, transform: 'translate(0, 0) rotate(0deg) scale(.78)', filter: 'blur(28px)' }];
    default:
      return [...entrance].reverse();
  }
}

export async function animateMediaElement(
  element: HTMLElement,
  type: CellAnimationType,
  durationMilliseconds: number,
  entering: boolean,
): Promise<void> {
  element.style.opacity = '1';
  if (type === 'none' || durationMilliseconds <= 0 || prefersReducedMotion()) return;

  const animation = element.animate(mediaAnimationKeyframes(type, entering), {
    duration: durationMilliseconds,
    easing: type === 'bounce' ? 'cubic-bezier(.22,.82,.32,1.18)' : 'cubic-bezier(.22,.72,.2,1)',
    fill: 'forwards',
  });
  try {
    await animation.finished;
    if (entering) {
      element.style.opacity = '1';
      animation.cancel();
    }
  } catch {
    // Replacing, clearing, or replaying media cancels an in-flight animation.
  }
}

function entranceKeyframes(type: CellAnimationType): Keyframe[] {
  switch (type) {
    case 'none':
      return [visible, visible];
    case 'fade':
      return [{ opacity: 0, transform: 'translate(0, 0) rotate(0deg) scale(1)' }, visible];
    case 'slide':
      return [{ opacity: 0, transform: 'translateY(28%) rotate(0deg) scale(.96)' }, visible];
    case 'zoom':
      return [{ opacity: 0, transform: 'translate(0, 0) rotate(0deg) scale(.62)' }, visible];
    case 'spin':
      return [
        { opacity: 0, transform: 'translate(0, 0) rotate(-540deg) scale(.16)' },
        { offset: .76, opacity: 1, transform: 'translate(0, 0) rotate(14deg) scale(1.08)' },
        visible,
      ];
    case 'flip':
      return [
        { opacity: 0, transform: 'perspective(700px) rotateY(-96deg) scale(.82)' },
        { offset: .78, opacity: 1, transform: 'perspective(700px) rotateY(10deg) scale(1.03)' },
        { ...visible, transform: 'perspective(700px) rotateY(0deg) scale(1)' },
      ];
    case 'bounce':
      return [
        { opacity: 0, transform: 'translateY(72%) rotate(0deg) scale(.72)' },
        { offset: .58, opacity: 1, transform: 'translateY(-14%) rotate(0deg) scale(1.04)' },
        { offset: .78, opacity: 1, transform: 'translateY(5%) rotate(0deg) scale(.98)' },
        visible,
      ];
    case 'morph':
      return [
        { opacity: 0, transform: 'translate(0, 0) rotate(-42deg) scale(.2)', borderRadius: '50%', clipPath: 'circle(8% at 50% 50%)' },
        { offset: .5, opacity: 1, transform: 'translate(0, 0) rotate(-10deg) scale(1.08)', borderRadius: '42% 58% 65% 35% / 38% 35% 65% 62%', clipPath: 'polygon(50% 0%, 96% 28%, 82% 88%, 20% 96%, 2% 38%)' },
        { ...visible, borderRadius: '0%', clipPath: 'inset(0% round 0%)' },
      ];
    case 'orbit':
      return [
        { opacity: 0, transform: 'translate(-58%, -24%) rotate(-420deg) scale(.18)' },
        { offset: .28, opacity: .65, transform: 'translate(-32%, 44%) rotate(-280deg) scale(.5)' },
        { offset: .55, opacity: .9, transform: 'translate(54%, 34%) rotate(-190deg) scale(.72)' },
        { offset: .8, opacity: 1, transform: 'translate(38%, -28%) rotate(-90deg) scale(.9)' },
        visible,
      ];
    case 'blur':
      return [
        { opacity: 0, transform: 'translate(0, 0) rotate(0deg) scale(1.22)', filter: 'blur(28px)' },
        { ...visible, filter: 'blur(0)' },
      ];
  }
}

function prefersReducedMotion(): boolean {
  return typeof window !== 'undefined'
    && typeof window.matchMedia === 'function'
    && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
}
