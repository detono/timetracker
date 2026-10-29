import { afterEach, describe, expect, it, vi } from "vitest";
import { formatCoordinates, mapUrl, reverseGeocode } from "../geolocation";

const here = { latitude: 51.2194475, longitude: 4.4024643 };

function mockFetch(response: { ok: boolean; body?: unknown } | Error) {
  vi.stubGlobal(
    "fetch",
    vi.fn(async () => {
      if (response instanceof Error) throw response;
      return { ok: response.ok, json: async () => response.body } as Response;
    })
  );
}

afterEach(() => {
  vi.unstubAllGlobals();
});

describe("reverseGeocode", () => {
  it("turns an OpenStreetMap address into a short street address", async () => {
    mockFetch({
      ok: true,
      body: {
        display_name: "long name",
        address: { shop: "Bakkerij", road: "Kerkstraat", house_number: "12", postcode: "2000", city: "Antwerpen" }
      }
    });

    expect(await reverseGeocode(here, "nl")).toBe("Bakkerij, Kerkstraat 12, 2000 Antwerpen");
    const url = String(vi.mocked(fetch).mock.calls[0][0]);
    expect(url).toContain("accept-language=nl");
    expect(url).toContain("lat=51.2194475");
  });

  it("falls back to the full name when there are no address parts", async () => {
    mockFetch({ ok: true, body: { display_name: "Somewhere in a field" } });

    expect(await reverseGeocode(here, "en")).toBe("Somewhere in a field");
  });

  it("returns null when the lookup fails, so the coordinates are used instead", async () => {
    mockFetch({ ok: false });
    expect(await reverseGeocode(here, "en")).toBeNull();

    mockFetch(new Error("offline"));
    expect(await reverseGeocode(here, "en")).toBeNull();
  });
});

describe("coordinates", () => {
  it("formats coordinates and links to the map", () => {
    expect(formatCoordinates(here)).toBe("51.21945, 4.40246");
    expect(mapUrl(here)).toBe(
      "https://www.openstreetmap.org/?mlat=51.2194475&mlon=4.4024643#map=17/51.2194475/4.4024643"
    );
  });
});
