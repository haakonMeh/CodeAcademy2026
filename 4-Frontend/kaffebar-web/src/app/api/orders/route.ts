import { NextResponse, type NextRequest } from "next/server";
import { createOrder, fetchOrders } from "@/lib/api";
import type { CreateOrderRequest, OrderStatus } from "@/types/domain";

export const dynamic = "force-dynamic";

/**
 * BFF-laget. Nettleseren snakker med denne ruta, ikke med Kaffebar-API-et —
 * da slipper vi CORS, og KAFFEBAR_API_URL forblir en serverhemmelighet.
 *
 * Utlevert. Den bruker funksjonene du skriver i src/lib/api.ts.
 */

export async function GET(request: NextRequest) {
  const status = request.nextUrl.searchParams.get("status") as OrderStatus | null;

  try {
    const orders = await fetchOrders({ ...(status ? { status } : {}) });
    return NextResponse.json(orders);
  } catch (error) {
    return NextResponse.json({ message: messageOf(error) }, { status: 502 });
  }
}

export async function POST(request: NextRequest) {
  const body = (await request.json()) as CreateOrderRequest;

  try {
    const order = await createOrder(body);
    return NextResponse.json(order, { status: 201 });
  } catch (error) {
    // Feilmeldingen kommer fra Problem Details-objektet API-et svarte med.
    return NextResponse.json({ message: messageOf(error) }, { status: 400 });
  }
}

function messageOf(error: unknown): string {
  return error instanceof Error ? error.message : "Ukjent feil";
}
