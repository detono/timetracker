import { afterEach, describe, expect, it } from "vitest";
import type { InternalAxiosRequestConfig } from "axios";
import { apiClient } from "../client";
import { assignSupervisor } from "../usersApi";

describe("assignSupervisor", () => {
  const originalAdapter = apiClient.defaults.adapter;
  afterEach(() => {
    apiClient.defaults.adapter = originalAdapter;
  });

  async function sent(supervisorId: string | null) {
    let request: InternalAxiosRequestConfig | undefined;
    apiClient.defaults.adapter = async (config) => {
      request = config;
      return { data: "", status: 204, statusText: "No Content", headers: {}, config };
    };
    await assignSupervisor("emp-1", supervisorId);
    return { contentType: request!.headers["Content-Type"], body: request!.data };
  }

  // A bare string used to go out unquoted as form data, which the API refused (415).
  it("sends the supervisor id as a JSON string", async () => {
    expect(await sent("3f2b1c9e-0000-0000-0000-000000000001")).toEqual({
      contentType: "application/json",
      body: '"3f2b1c9e-0000-0000-0000-000000000001"'
    });
  });

  it("sends JSON null to clear the supervisor", async () => {
    expect(await sent(null)).toEqual({ contentType: "application/json", body: "null" });
  });
});
