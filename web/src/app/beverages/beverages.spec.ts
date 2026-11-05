import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Beverages } from './beverages';

describe('Beverages', () => {
  let fixture: ComponentFixture<Beverages>;
  let saved: number[];

  beforeEach(async () => {
    fixture = TestBed.createComponent(Beverages);
    saved = [];
    fixture.componentInstance.save.subscribe((count) => saved.push(count));
    fixture.componentRef.setInput('value', 3);
    fixture.componentRef.setInput('disabled', false);
    await settle();
  });

  async function settle(): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  function field(): HTMLInputElement {
    return (fixture.nativeElement as HTMLElement).querySelector('input')!;
  }

  function button(label: string): HTMLButtonElement {
    return (fixture.nativeElement as HTMLElement).querySelector(`button[aria-label="${label}"]`)!;
  }

  async function type(text: string): Promise<void> {
    field().value = text;
    field().dispatchEvent(new Event('change'));
    await settle();
  }

  it('rejects_negative_and_over_999', async () => {
    for (const text of ['-1', '1000', '1.5', '']) {
      await type(text);
      expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')?.textContent).toContain('whole number from 0 to 999');
    }
    expect(saved).toEqual([]);
  });

  it('emits_valid_value_and_clears_message', async () => {
    await type('-1');
    await type('999');

    expect(saved).toEqual([999]);
    expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')).toBeNull();
  });

  it('plus_and_minus_step_and_clamp', async () => {
    button('More beverages').click();
    button('Fewer beverages').click();
    fixture.componentRef.setInput('value', 0);
    await settle();
    button('Fewer beverages').click();
    fixture.componentRef.setInput('value', 999);
    await settle();
    button('More beverages').click();

    expect(saved).toEqual([4, 2]);
  });
});
