import { NextResponse, type NextRequest } from "next/server";
import { updateOrderStatus } from "@/lib/api";
import type { OrderStatus } from "@/types/domain";

export const dynamic = "force-dynamic";

/**
 * BFF for baristaknappene. Utlevert — bruker updateOrderStatus fra src/lib/api.ts.
 */
export async function PATCH(
  request: NextRequest,
  context: { params: Promise<{ orderId: string }> },
) {
  const { orderId } = await context.params;
  const { status } = (await request.json()) as { status: OrderStatus };

  try {
    return NextResponse.json(await updateOrderStatus(orderId, status));
  } catch (error) {
    const message = error instanceof Error ? error.message : "Ukjent feil";
    return NextResponse.json({ message }, { status: 502 });
  }
}
