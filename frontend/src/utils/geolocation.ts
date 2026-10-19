export interface Coordinates {
  latitude: number;
  longitude: number;
}

export type GeolocationErrorCode = "unsupported" | "denied" | "unavailable" | "timeout";

export class GeolocationError extends Error {
  constructor(public code: GeolocationErrorCode) {
    super(code);
  }
}

/** Asks the browser for the current position. Triggers the permission prompt on first use. */
export function getCurrentPosition(): Promise<Coordinates> {
  return new Promise((resolve, reject) => {
    // The Geolocation API is only exposed on secure origins (https or localhost).
    if (!("geolocation" in navigator) || !window.isSecureContext) {
      reject(new GeolocationError("unsupported"));
      return;
    }

    navigator.geolocation.getCurrentPosition(
      (pos) => resolve({ latitude: pos.coords.latitude, longitude: pos.coords.longitude }),
      (err) => {
        const code: GeolocationErrorCode =
          err.code === err.PERMISSION_DENIED ? "denied" : err.code === err.TIMEOUT ? "timeout" : "unavailable";
        reject(new GeolocationError(code));
      },
      { enableHighAccuracy: true, timeout: 15000, maximumAge: 60000 }
    );
  });
}

interface NominatimResponse {
  display_name?: string;
  address?: Record<string, string>;
}

/**
 * Turns coordinates into a short street address using OpenStreetMap's free Nominatim
 * service. Returns null if the lookup fails so callers can fall back to raw coordinates.
 */
export async function reverseGeocode({ latitude, longitude }: Coordinates, language: string): Promise<string | null> {
  const params = new URLSearchParams({
    format: "jsonv2",
    lat: String(latitude),
    lon: String(longitude),
    zoom: "18",
    addressdetails: "1",
    "accept-language": language
  });

  try {
    const res = await fetch(`https://nominatim.openstreetmap.org/reverse?${params}`);
    if (!res.ok) return null;
    const data: NominatimResponse = await res.json();
    return formatAddress(data);
  } catch {
    return null;
  }
}

function formatAddress({ address, display_name }: NominatimResponse): string | null {
  if (!address) return display_name ?? null;

  const street = [address.road ?? address.pedestrian ?? address.footway, address.house_number]
    .filter(Boolean)
    .join(" ");
  const place = address.city ?? address.town ?? address.village ?? address.municipality ?? address.hamlet;
  const locality = [address.postcode, place].filter(Boolean).join(" ");
  const short = [address.amenity ?? address.building ?? address.shop, street, locality].filter(Boolean).join(", ");

  return short || display_name || null;
}

export function formatCoordinates({ latitude, longitude }: Coordinates): string {
  return `${latitude.toFixed(5)}, ${longitude.toFixed(5)}`;
}

export function mapUrl({ latitude, longitude }: Coordinates): string {
  return `https://www.openstreetmap.org/?mlat=${latitude}&mlon=${longitude}#map=17/${latitude}/${longitude}`;
}
