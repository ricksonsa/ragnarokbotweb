export class Page<T> {
    number!: number;
    size!: number;
    totalElements!: number;
    totalPages!: number;
    content!: T[];
  }
  