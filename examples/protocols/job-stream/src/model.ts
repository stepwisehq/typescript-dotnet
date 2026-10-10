export interface Job { id: string; weight: number; }
export class Session implements Disposable {
  [Symbol.dispose](): void {}
}
export class AsyncSession implements AsyncDisposable {
  async [Symbol.asyncDispose](): Promise<void> {}
}
