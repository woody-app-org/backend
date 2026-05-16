import http from "k6/http";
import { check } from "k6";

export const options = {
  scenarios: {
    duplicate_race: {
      executor: "constant-vus",
      vus: 20,
      duration: "5s",
    },
  },
};

const API_BASE_URL = __ENV.API_BASE_URL || "http://localhost:5000";

export default function () {
  const payload = JSON.stringify({
    name: "Pessoa Duplicada",
    socialNetwork: "instagram",
    socialUsername: "@mesma_pessoa_teste",
    acceptedContact: true,
    website: "",
  });

  const res = http.post(`${API_BASE_URL}/api/prelaunch/signups`, payload, {
    headers: {
      "Content-Type": "application/json",
    },
  });

  check(res, {
    "retorna sucesso idempotente ou 429": (r) =>
      r.status === 200 || r.status === 201 || r.status === 429,
    "não retorna 500": (r) => r.status < 500,
  });
}