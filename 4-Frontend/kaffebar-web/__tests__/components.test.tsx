import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { StatusBadge } from "@/components/StatusBadge";
import { StatusColumn } from "@/components/StatusColumn";
import { nextStatus } from "@/components/OrderCard";
import { formatPrice, timeAgo } from "@/lib/format";
import type { Order } from "@/types/domain";

const order: Order = {
  orderId: "0de50001-0000-4000-8000-000000000001",
  coffeeId: "c0ffee01-0000-4000-8000-000000000007",
  coffeeName: "Kaffe Latte",
  customerName: "Ada",
  size: "MEDIUM",
  milkType: "OAT",
  quantity: 1,
  status: "PENDING",
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
};

describe("StatusBadge", () => {
  it("viser norsk etikett for statusen", () => {
    render(<StatusBadge status="BREWING" />);
    expect(screen.getByText("Under arbeid")).toBeInTheDocument();
  });
});

describe("StatusColumn", () => {
  it("viser tom tilstand når kolonnen er tom", () => {
    render(<StatusColumn status="READY" orders={[]} />);
    expect(screen.getByText("Alt er hentet.")).toBeInTheDocument();
  });

  it("teller ordrene i kolonnen", () => {
    render(<StatusColumn status="PENDING" orders={[order]} />);
    expect(screen.getByText("1")).toBeInTheDocument();
    expect(screen.getByText("Ada")).toBeInTheDocument();
  });
});

describe("statusflyten", () => {
  it("går PENDING -> BREWING -> READY og stopper der", () => {
    expect(nextStatus("PENDING")).toBe("BREWING");
    expect(nextStatus("BREWING")).toBe("READY");
    expect(nextStatus("READY")).toBeNull();
  });
});

describe("formatering", () => {
  it("viser pris i norske kroner", () => {
    expect(formatPrice(52)).toMatch(/52/);
  });

  it("sier «nå» om noe som skjedde nettopp", () => {
    expect(timeAgo(new Date().toISOString())).toBe("nå");
  });
});
