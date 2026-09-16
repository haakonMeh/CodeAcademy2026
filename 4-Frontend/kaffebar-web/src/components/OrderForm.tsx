"use client";

import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { MenuCard } from "./MenuCard";
import { EmptyState } from "./EmptyState";
import { ORDERS_QUERY_KEY } from "@/hooks/useOrders";
import {
  MILK_LABEL,
  SIZE_LABEL,
  type Coffee,
  type CoffeeSize,
  type CreateOrderRequest,
  type MilkType,
  type Order,
} from "@/types/domain";

const SIZES: CoffeeSize[] = ["SMALL", "MEDIUM", "LARGE"];
const MILKS: MilkType[] = ["WHOLE", "SKIMMED", "OAT", "SOY"];

/**
 * Bestillingsskjemaet. Utlevert.
 *
 * Legg merke til at den poster til /api/orders — vår egen route handler — og
 * ikke til Kaffebar-API-et direkte. Nettleseren vet ikke at Kaffebar-API-et
 * finnes; det er BFF-en som vet det.
 */
export function OrderForm({
  menu,
  onOrdered,
}: {
  menu: Coffee[];
  onOrdered?: (order: Order) => void;
}) {
  const queryClient = useQueryClient();
  const [selected, setSelected] = useState<Coffee | null>(menu[0] ?? null);
  const [customerName, setCustomerName] = useState("");
  const [size, setSize] = useState<CoffeeSize>("MEDIUM");
  const [milkType, setMilkType] = useState<MilkType | "">("");
  const [extraShot, setExtraShot] = useState(false);
  const [quantity, setQuantity] = useState(1);

  const mutation = useMutation({
    mutationFn: async (request: CreateOrderRequest) => {
      const response = await fetch("/api/orders", {
        method: "POST",
        headers: { "content-type": "application/json" },
        body: JSON.stringify(request),
      });
      const body = await response.json();
      if (!response.ok) throw new Error(body.message ?? "Bestillingen gikk ikke gjennom");
      return body as Order;
    },
    onSuccess: (order) => {
      setCustomerName("");
      setQuantity(1);
      void queryClient.invalidateQueries({ queryKey: ORDERS_QUERY_KEY });
      onOrdered?.(order);
    },
  });

  if (menu.length === 0) {
    return (
      <EmptyState
        icon="📋"
        title="Menyen er tom"
        description="Kjører Kaffebar-API-et? Eller start appen med USE_MOCK_API=true."
      />
    );
  }

  const submit = (event: React.FormEvent) => {
    event.preventDefault();
    if (!selected) return;

    mutation.mutate({
      coffeeId: selected.id,
      customerName: customerName.trim(),
      size,
      ...(milkType ? { milkType } : {}),
      ...(extraShot ? { extraShot } : {}),
      // `quantity` har default i kontrakten, men openapi-typescript gjør felt
      // med default-verdi påkrevde i TypeScript. Vi sender den alltid.
      quantity,
    });
  };

  return (
    <form onSubmit={submit} className="flex flex-col gap-5">
      <fieldset className="flex flex-col gap-2">
        <legend className="mb-2 text-sm font-semibold">Velg kaffe</legend>
        <div className="grid gap-2 sm:grid-cols-2">
          {menu.map((coffee) => (
            <MenuCard
              key={coffee.id}
              coffee={coffee}
              selected={selected?.id === coffee.id}
              onSelect={setSelected}
            />
          ))}
        </div>
      </fieldset>

      <div className="grid gap-4 sm:grid-cols-2">
        <label className="flex flex-col gap-1.5">
          <span className="text-sm font-semibold">Navn</span>
          <input
            value={customerName}
            onChange={(event) => setCustomerName(event.target.value)}
            required
            minLength={2}
            maxLength={50}
            placeholder="Hvem ropes opp?"
            className="rounded-lg border border-line bg-card px-3 py-2"
          />
        </label>

        <label className="flex flex-col gap-1.5">
          <span className="text-sm font-semibold">Størrelse</span>
          <select
            value={size}
            onChange={(event) => setSize(event.target.value as CoffeeSize)}
            className="rounded-lg border border-line bg-card px-3 py-2"
          >
            {SIZES.map((value) => (
              <option key={value} value={value}>
                {SIZE_LABEL[value]}
              </option>
            ))}
          </select>
        </label>

        <label className="flex flex-col gap-1.5">
          <span className="text-sm font-semibold">Melk</span>
          <select
            value={milkType}
            onChange={(event) => setMilkType(event.target.value as MilkType | "")}
            className="rounded-lg border border-line bg-card px-3 py-2"
          >
            <option value="">Ingen</option>
            {MILKS.map((value) => (
              <option key={value} value={value}>
                {MILK_LABEL[value]}
              </option>
            ))}
          </select>
        </label>

        <label className="flex flex-col gap-1.5">
          <span className="text-sm font-semibold">Antall</span>
          <input
            type="number"
            min={1}
            max={10}
            value={quantity}
            onChange={(event) => setQuantity(Number(event.target.value))}
            className="rounded-lg border border-line bg-card px-3 py-2"
          />
        </label>
      </div>

      <label className="flex items-center gap-2 text-sm">
        <input
          type="checkbox"
          checked={extraShot}
          onChange={(event) => setExtraShot(event.target.checked)}
          className="h-4 w-4 accent-[var(--accent)]"
        />
        Ekstra shot
      </label>

      {mutation.isError && (
        <p role="alert" className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">
          {mutation.error.message}
        </p>
      )}

      {mutation.isSuccess && (
        <p className="rounded-lg bg-[var(--status-ready-soft)] px-3 py-2 text-sm text-[var(--status-ready)]">
          Bestilt! Følg den på baristaskjermen.
        </p>
      )}

      <button
        type="submit"
        disabled={mutation.isPending || !selected || customerName.trim().length < 2}
        className="rounded-lg bg-accent px-4 py-2.5 font-medium text-white transition-colors hover:bg-accent-strong disabled:cursor-not-allowed disabled:opacity-50"
      >
        {mutation.isPending ? "Bestiller …" : "Bestill"}
      </button>
    </form>
  );
}
