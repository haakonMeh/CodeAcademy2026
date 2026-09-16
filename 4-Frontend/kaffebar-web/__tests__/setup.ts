import "@testing-library/jest-dom/vitest";
import { cleanup } from "@testing-library/react";
import { afterEach, vi } from "vitest";

// jsdom har ingen EventSource. Testene under trenger den ikke, men komponentene
// importerer hooken som bruker den.
class MockEventSource {
  static readonly CLOSED = 2;
  readyState = 0;
  addEventListener(): void {}
  removeEventListener(): void {}
  close(): void {}
}

vi.stubGlobal("EventSource", MockEventSource);

afterEach(() => {
  cleanup();
});
