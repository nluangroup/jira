import { TestBed } from '@angular/core/testing';
import { Identity } from './identity';

describe('Identity', () => {
  beforeEach(() => localStorage.clear());

  it('treats a corrupt stored value as no identity', () => {
    localStorage.setItem('lunch.employee', '{not json');

    expect(TestBed.inject(Identity).current()).toBeNull();
  });
});
