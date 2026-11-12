import { AfterViewInit, Component, ElementRef, Input, OnChanges, OnDestroy, SimpleChanges, ViewChild, signal } from '@angular/core';

import { CellAnimationType } from '../../../../../core/backend-api.service';
import { TextPipe } from '../../../../../core/i18n/text.pipe';
import { animateMediaElement } from '../../../../../shared/media/media-animation';

@Component({
  selector: 'td-animation-playground',
  standalone: true,
  imports: [TextPipe],
  templateUrl: './animation-playground.component.html',
  styleUrl: './animation-playground.component.css',
})
export class AnimationPlaygroundComponent implements AfterViewInit, OnChanges, OnDestroy {
  @ViewChild('sample', { static: true }) private sample!: ElementRef<HTMLElement>;

  @Input() animationInType: CellAnimationType = 'none';
  @Input() animationInDurationMilliseconds = 0;
  @Input() animationOutType: CellAnimationType = 'none';
  @Input() animationOutDurationMilliseconds = 0;

  readonly playingDirection = signal<'in' | 'out' | null>(null);

  private initialized = false;
  private replayTimer?: ReturnType<typeof setTimeout>;

  ngAfterViewInit(): void {
    this.initialized = true;
    this.scheduleEntrancePreview();
  }

  ngOnChanges(_changes: SimpleChanges): void {
    if (this.initialized) this.scheduleEntrancePreview();
  }

  ngOnDestroy(): void {
    if (this.replayTimer) clearTimeout(this.replayTimer);
    this.sample.nativeElement.getAnimations().forEach(animation => animation.cancel());
  }

  async preview(direction: 'in' | 'out'): Promise<void> {
    if (this.replayTimer) clearTimeout(this.replayTimer);
    const element = this.sample.nativeElement;
    this.resetSample();
    this.playingDirection.set(direction);
    const entering = direction === 'in';
    const type = entering ? this.animationInType : this.animationOutType;
    const configuredDuration = entering ? this.animationInDurationMilliseconds : this.animationOutDurationMilliseconds;
    const duration = Math.max(250, Number(configuredDuration) || 0);
    await animateMediaElement(element, type, duration, entering);
    if (!entering) {
      await new Promise<void>(resolve => setTimeout(resolve, 180));
      this.resetSample();
    }
    this.playingDirection.set(null);
  }

  private scheduleEntrancePreview(): void {
    if (this.replayTimer) clearTimeout(this.replayTimer);
    this.replayTimer = setTimeout(() => void this.preview('in'), 100);
  }

  private resetSample(): void {
    const element = this.sample.nativeElement;
    element.getAnimations().forEach(animation => animation.cancel());
    element.removeAttribute('style');
    element.style.opacity = '1';
  }
}
