import http from "k6/http";
import { check, sleep } from "k6";

export const options = {
  scenarios: {
    concurrent_signups: {
      executor: "constant-vus",
      vus: 30,
      duration: "20s",
    },
  },
};

const API_BASE_URL = __ENV.API_BASE_URL || "http://localhost:5000";

export default function () {
  const unique = `${__VU}-${__ITER}-${Date.now()}`;

  const payload = JSON.stringify({
    name: `Teste ${unique}`,
    socialNetwork: "instagram",
    socialUsername: `woody_teste_${unique}`,
    acceptedContact: true,
    website: "",
  });

  const res = http.post(`${API_BASE_URL}/api/prelaunch/signups`, payload, {
    headers: {
      "Content-Type": "application/json",
    },
  });

  check(res, {
    "status é 200/201 ou 429": (r) =>
      r.status === 200 || r.status === 201 || r.status === 429,
    "não retornou 500": (r) => r.status < 500,
  });

  sleep(0.2);
}