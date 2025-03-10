import { BehaviorSubject, Subject } from 'rxjs';
import { DOCUMENT } from '@angular/common';
import { Inject, Injectable, Renderer2 } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private style: HTMLLinkElement;
  private cssFile: string;
  private themeCSSID: string = 'themeCSS';
  constructor(
    @Inject(DOCUMENT) private document: Document
  ) {
    this.responsiveWatch();
  }

  private fullSizeBehaviourSubject = new BehaviorSubject(false);
  private themeChangeBehaviourSubject: Subject<string> = new Subject();
  private responsiveBehaviourSubject = new BehaviorSubject({ width: 1024, responsive: false });

  private removeExistingThemeStyle(renderer2: Renderer2, themeCSSID: string) {
    const themeIDHTMlElem = this.document.getElementById(themeCSSID);
    if (themeIDHTMlElem) {
      renderer2.removeChild(this.document.head, themeIDHTMlElem);
    }
  }

  private responsiveWatch() {
    const resize_ob = new ResizeObserver((entries) => {
      // since we are observing only a single element, so we access the first element in entries array
      let rect = entries[0].contentRect;

      // current width & height
      let width = rect.width;
      let height = rect.height;

      // console.log('Current Width : ' + width);
      // console.log('Current Height : ' + height);
      this.responsiveBehaviourSubject.next({ width, responsive: width <= 768 });
    });

    // start observing for resize
    resize_ob.observe(document.getElementsByTagName('html')[0]);
  }

  public isFullSize() {
    return this.fullSizeBehaviourSubject.value;
  }

  public isResponsiveObservable() {
    return this.responsiveBehaviourSubject.asObservable();
  }

  public isResponsive() {
    return this.responsiveBehaviourSubject.value;
  }

  public isFullSizeObservable() {
    return this.fullSizeBehaviourSubject.asObservable();
  }

  public setFullSize(value: boolean) {
    this.fullSizeBehaviourSubject.next(value);
  }

  public toogleContrast() {
    const dom = this.document.getElementsByClassName('html')[0];
    if (dom.classList.contains('contrasted')) {
      dom.classList.remove('contrasted');
      return false;
    }
    dom.classList.add('contrasted');
    return true;
  }

  public onThemeChange() {
    return this.themeChangeBehaviourSubject.asObservable();
  }

  public setTheme(theme: string, renderer2: Renderer2) {
    this.themeChangeBehaviourSubject.next(theme);

    this.cssFile = `${theme}.css`;
    this.removeExistingThemeStyle(renderer2, this.themeCSSID);

    // Create a link element via Angular's renderer to avoid SSR troubles
    this.style = renderer2.createElement('link') as HTMLLinkElement;

    // Set type of the link item and path to the css file
    renderer2.setProperty(this.style, 'rel', 'stylesheet');
    renderer2.setProperty(this.style, 'href', this.cssFile);
    renderer2.setProperty(this.style, 'id', this.themeCSSID);

    // Add the style to the head section
    renderer2.appendChild(this.document.head, this.style);
  }
}