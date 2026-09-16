"use client";

import { useState } from "react";
import { OrderForm } from "./OrderForm";
import { MyOrderTracker } from "./MyOrderTracker";
import type { Coffee, Order } from "@/types/domain";

/** Limet mellom skjemaet og «din bestilling»-kortet. Utlevert. */
export function CustomerScreen({
  menu,
  initialOrders,
}: {
  menu: Coffee[];
  initialOrders: Order[];
}) {
  const [myOrderId, setMyOrderId] = useState<string | null>(null);

  return (
    <div className="grid gap-8 lg:grid-cols-[1fr_20rem]">
      <OrderForm menu={menu} onOrdered={(order) => setMyOrderId(order.orderId)} />
      <aside className="lg:sticky lg:top-6 lg:self-start">
        <MyOrderTracker orderId={myOrderId} initialOrders={initialOrders} />
      </aside>
    </div>
  );
}
