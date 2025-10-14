# Animated Dialogs - Przewodnik Użycia

## Wprowadzenie

Ten przewodnik opisuje jak używać nowoczesnych animacji shape morphing dla okien dialogowych PrimeNG w projekcie Angular 19.

**Ważne**: Animacje są zrealizowane przy użyciu CSS animations (nie Angular animations), ponieważ PrimeNG dialogi są renderowane dynamicznie jako overlays w DOM, co sprawia, że CSS animations są najbardziej efektywnym rozwiązaniem.

## Cechy Animacji

- **Shape Morphing**: Płynna transformacja z scale(0.8) do scale(1) z efektem okrągłego przejścia
- **Czas trwania**: 600ms dla pojawienia się
- **Easing**: cubic-bezier(0.4, 0, 0.2, 1) dla organicznego efektu
- **Backdrop Blur**: Rozmycie tła z filtrem 8px
- **Wsparcie dla Dark Mode**: Automatyczne dostosowanie kolorów
- **Accessibility**: Respektuje preferencje użytkownika (reduced motion)
- **Automatyczne**: Nie wymaga dodatkowego kodu TypeScript - działa od razu!

## Struktura Plików

```
angular/src/app/shared/
└── styles/
    └── animated-dialog.scss       # Style CSS z @keyframes animations
```

**Uwaga**: Plik `dialog.animations.ts` nie jest już używany - animacje są w pełni oparte na CSS.

## Implementacja w Komponencie

### Superproste! Tylko Jeden Krok

Animacje działają automatycznie dzięki CSS. Wystarczy dodać klasę `animated-dialog` do PrimeNG Dialog:

```html
<!-- PRZED: -->
<p-dialog
  [(visible)]="visible"
  [modal]="true"
  header="My Dialog">
  <!-- zawartość -->
</p-dialog>

<!-- PO: -->
<p-dialog
  [(visible)]="visible"
  [modal]="true"
  [styleClass]="'animated-dialog'"
  header="My Dialog">
  <!-- zawartość -->
</p-dialog>
```

**To wszystko!** Nie musisz:
- ❌ Importować żadnych animacji w TypeScript
- ❌ Dodawać `animations` do dekoratora `@Component`
- ❌ Tworzyć wrapper divów
- ❌ Pisać dodatkowego kodu

✅ **Wystarczy dodać** `[styleClass]="'animated-dialog'"` do `<p-dialog>`

## Jak To Działa?

Animacje są zdefiniowane w pliku `animated-dialog.scss` jako CSS `@keyframes`:

- **`dialogShapeMorph`**: Główna animacja shape morphing
- **`backdropFadeIn`**: Animacja tła dialogu
- Automatycznie aplikowane do wszystkich `.p-dialog` elementów

Wszystko działa automatycznie gdy dodasz klasę `animated-dialog` do dialogu PrimeNG!

## Przykłady Implementacji

### Przykład 1: Prosty Dialog

```typescript
// component.ts
import { Component } from '@angular/core';

@Component({
  selector: 'app-simple-dialog',
  templateUrl: './simple-dialog.component.html'
})
export class SimpleDialogComponent {
  visible = false;

  show() {
    this.visible = true;
  }

  hide() {
    this.visible = false;
  }
}
```

```html
<!-- component.html -->
<button (click)="show()">Open Dialog</button>

<p-dialog
  [(visible)]="visible"
  [modal]="true"
  [styleClass]="'animated-dialog'"
  header="Simple Dialog"
  (onHide)="hide()">

  <p>This is a simple animated dialog.</p>

  <ng-template pTemplate="footer">
    <button pButton label="Close" (click)="hide()"></button>
  </ng-template>
</p-dialog>
```

### Przykład 2: Dialog z Formularzem

```typescript
// edit-item-dialog.component.ts
import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-edit-item-dialog',
  templateUrl: './edit-item-dialog.component.html'
})
export class EditItemDialogComponent {
  @Input() visible = false;
  @Input() item: any;
  @Output() visibleChange = new EventEmitter<boolean>();
  @Output() saved = new EventEmitter<any>();

  close() {
    this.visible = false;
    this.visibleChange.emit(false);
  }

  save() {
    // Zapisz dane
    this.saved.emit(this.item);
    this.close();
  }
}
```

```html
<!-- edit-item-dialog.component.html -->
<p-dialog
  [(visible)]="visible"
  [modal]="true"
  [styleClass]="'animated-dialog'"
  [style]="{width: '600px'}"
  header="Edit Item"
  (onHide)="close()">

  <form>
    <!-- Pola formularza -->
    <div class="field">
      <label>Name</label>
      <input pInputText [(ngModel)]="item.name" />
    </div>
  </form>

  <ng-template pTemplate="footer">
    <button pButton label="Cancel" class="p-button-text" (click)="close()"></button>
    <button pButton label="Save" (click)="save()"></button>
  </ng-template>
</p-dialog>
```

## Customizacja Stylów

### Zmiana Kolorów Nagłówka

Edytuj plik `angular/src/app/shared/styles/animated-dialog.scss`:

```scss
.p-dialog {
  &.animated-dialog {
    .p-dialog-header {
      // Domyślny gradient
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);

      // Twój własny gradient
      background: linear-gradient(135deg, #ff6b6b 0%, #ee5a6f 100%);

      // Lub jednolity kolor
      background: #007bff;
    }
  }
}
```

### Zmiana Rozmiaru Rozmycia

```scss
.p-dialog-mask {
  // Domyślnie 8px
  backdrop-filter: blur(8px);

  // Silniejsze rozmycie
  backdrop-filter: blur(16px);

  // Słabsze rozmycie
  backdrop-filter: blur(4px);
}
```

### Dostosowanie Prędkości Animacji

Edytuj plik `angular/src/app/shared/styles/animated-dialog.scss`:

```scss
::ng-deep {
  .p-dialog {
    // Wolniejsza animacja
    animation: dialogShapeMorph 800ms cubic-bezier(0.4, 0, 0.2, 1);

    // Szybsza animacja
    animation: dialogShapeMorph 400ms cubic-bezier(0.4, 0, 0.2, 1);
  }
}
```

## Najlepsze Praktyki

### 1. Style Class

Zawsze dodawaj `styleClass` dla pełnych efektów:

```html
<!-- ✅ DOBRZE - Pełna animacja i gradient -->
<p-dialog [styleClass]="'animated-dialog'">

<!-- ⚠️ Podstawowa animacja bez gradientu -->
<p-dialog>
```

Uwaga: Nawet bez `styleClass="animated-dialog"`, wszystkie dialogi otrzymają animację shape morphing. Klasa dodaje dodatkowe style (gradient w nagłówku, lepsze kolory).

### 2. Focus Management

PrimeNG automatycznie zarządza focus trap - nie wymaga dodatkowej konfiguracji.

### 3. Performance

CSS animations są bardzo wydajne! Nie musisz się martwić o performance - animacje są akcelerowane sprzętowo przez GPU.

## Rozwiązywanie Problemów

### Problem: Animacja nie działa

**Rozwiązanie**: Upewnij się, że:
1. Plik `animated-dialog.scss` jest zaimportowany w `styles.scss`
2. Dialog ma `[styleClass]="'animated-dialog'"` (opcjonalne, ale zalecane)
3. Przeglądarka wspiera CSS animations (praktycznie wszystkie nowoczesne przeglądarki)

### Problem: Rozmycie tła nie działa

**Rozwiązanie**:
- `backdrop-filter` nie jest wspierany we wszystkich przeglądarkach
- W starszych przeglądarkach będzie użyty fallback z ciemniejszym tłem

### Problem: Animacja jest zbyt szybka/wolna

**Rozwiązanie**: Dostosuj czas w pliku `animated-dialog.scss`:

```scss
::ng-deep {
  .p-dialog {
    animation: dialogShapeMorph YOUR_TIME_MS cubic-bezier(0.4, 0, 0.2, 1);
  }
}
```

## Wsparcie dla Accessibility

Animacje automatycznie respektują preferencje użytkownika:

```scss
@media (prefers-reduced-motion: reduce) {
  // Animacje są wyłączone dla użytkowników z preferencją reduced motion
}
```

## Komponenty z Implementacją

W projekcie już zaimplementowano animacje w następujących komponentach:

1. **EditCartItemDialogComponent**
   - Lokalizacja: `angular/src/app/cart/edit-cart-item-dialog/`
   - Użycie: Dialog edycji elementu koszyka

2. **AdminBoothRentalDialogComponent**
   - Lokalizacja: `angular/src/app/booth/admin-booth-rental-dialog/`
   - Użycie: Dialog zarządzania wynajmem stoiska

## Dodatkowe Zasoby

- [Angular Animations Guide](https://angular.io/guide/animations)
- [PrimeNG Dialog Documentation](https://primeng.org/dialog)
- [CSS backdrop-filter MDN](https://developer.mozilla.org/en-US/docs/Web/CSS/backdrop-filter)

## Podsumowanie

Animowane dialogi dodają nowoczesny, profesjonalny wygląd do aplikacji. Implementacja jest **super prosta**:

1. ✅ Dodaj `[styleClass]="'animated-dialog'"` do `<p-dialog>`
2. ✅ Gotowe!

**Poważnie, to wszystko!** CSS animations działają automatycznie - nie potrzebujesz żadnego dodatkowego kodu TypeScript ani wrapperów.

---

**Wersja**: 1.0
**Data**: 2025-10-10
**Autor**: MP Development Team
