import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-report',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<h1>Report</h1><p>Coming soon.</p>`,
})
export class Report {}
