import { useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import type { LoginDto, UserDto } from "../../types/account.types";
import { User, Lock } from "lucide-react";
import "./Login.css";

// -----------------------------------------------------------------------------
// Utility: parse many possible error payload shapes returned by an ASP.NET API
// - handles JSON bodies (objects, { message }, { errors }, arrays)
// - handles plain text bodies (Swagger sometimes returns plain text even on 500)
// - splits comma/newline/period-separated messages into a list
// - returns an array of strings (one entry per visible message)
// -----------------------------------------------------------------------------
async function parseErrorsFromResponse(response: Response): Promise<string[]> {
  if (!response) return ["Unknown error"];

  // small helpers
  const splitMessage = (text: string) => {
    const cleaned = (text || "").replace(/\s+/g, " ").trim();
    if (!cleaned) return [];
    // split on newlines, commas, or sentence endings. keep meaningful fragments.
    const parts = cleaned
      .split(/\r?\n|,\s+|\.\s+/)
      .map((p) => p.trim())
      .filter(Boolean);
    // ensure punctuation at end for readability
    return parts.map((p) => (/[.!?]$/.test(p) ? p : p + "."));
  };

  const flattenObjectValues = (obj: any): string[] => {
    const out: string[] = [];
    const queue = [obj];
    while (queue.length) {
      const cur = queue.shift();
      if (cur == null) continue;
      if (
        typeof cur === "string" ||
        typeof cur === "number" ||
        typeof cur === "boolean"
      ) {
        out.push(String(cur));
      } else if (Array.isArray(cur)) {
        cur.forEach((v) => queue.push(v));
      } else if (typeof cur === "object") {
        Object.values(cur).forEach((v) => queue.push(v));
      }
    }
    return out;
  };

  try {
    const contentType = (
      response.headers.get("content-type") || ""
    ).toLowerCase();

    // If JSON-like content-type, try parse as JSON
    if (
      contentType.includes("application/json") ||
      contentType.includes("text/json")
    ) {
      const json = await response.json();
      if (!json) return [`Request failed with status ${response.status}`];

      // Common shapes:
      // - a plain string
      if (typeof json === "string") return splitMessage(json);

      // - { message: "..." } or { title: "..." }
      if (typeof json.message === "string") return splitMessage(json.message);
      if (typeof json.title === "string") return splitMessage(json.title);

      // - { errors: { Field: ["err1", "err2"] } } or { errors: ["err1","err2"] }
      if (json.errors) {
        if (typeof json.errors === "string") return splitMessage(json.errors);
        if (Array.isArray(json.errors)) return json.errors.flat().map(String);
        if (typeof json.errors === "object") {
          const arr: string[] = [];
          Object.values(json.errors).forEach((v: any) => {
            if (Array.isArray(v)) arr.push(...v.map(String));
            else arr.push(String(v));
          });
          if (arr.length) return arr;
        }
      }

      // - other fields like 'error', 'detail'
      if (typeof json.error === "string") return splitMessage(json.error);
      if (typeof json.detail === "string") return splitMessage(json.detail);

      // - fallback: flatten values and return
      const flat = flattenObjectValues(json);
      if (flat.length) return flat.map(String);

      return [`Request failed with status ${response.status}`];
    }

    // Non-JSON: read as text (Swagger sometimes returns plain text even on 500)
    const text = (await response.text()).trim();
    if (!text) return [`Request failed with status ${response.status}`];

    return splitMessage(text);
  } catch (err) {
    // final fallback: try to extract text
    try {
      const text = (await response.text()).trim();
      if (text) return splitMessage(text);
    } catch (_e) {}
    return [`Request failed with status ${response.status}`];
  }
}

const LoginForm = () => {
  const { setUser } = useAuth();
  const navigate = useNavigate();
  const [form, setForm] = useState<LoginDto>({ username: "", password: "" });
  const [errors, setErrors] = useState<string[]>([]);
  const [loading, setLoading] = useState<boolean>(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrors([]);
    setLoading(true);

    try {
      const response = await fetch("http://localhost:5286/api/account/login", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(form),
      });

      if (!response.ok) {
        const parsed = await parseErrorsFromResponse(response);
        setErrors(parsed);
        setLoading(false);
        return;
      }

      const data = await response.json();

      const userData: UserDto = {
        userName: data.userName,
        email: data.email,
        token: data.token,
        isBlocked: data.isBlocked,
        role: data.role,
      };

      setUser(userData);
      localStorage.setItem("user", JSON.stringify(userData));

      if (userData.role === "Admin") {
        navigate("/admin/dashboard");
        return;
      }
      navigate("/");
    } catch (err: any) {
      setErrors([err?.message || "Login failed"]);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="auth-form-container">
      <h2>Login</h2>

      {errors.length > 0 && (
        <div className="error">
          <ul>
            {errors.map((err, idx) => (
              <li key={idx}>{err}</li>
            ))}
          </ul>
        </div>
      )}

      <form onSubmit={handleSubmit}>
        <div className="input-group">
          <User className="icon" />
          <input
            type="text"
            placeholder="Username"
            value={form.username}
            onChange={(e) => setForm({ ...form, username: e.target.value })}
            required
          />
        </div>

        <div className="input-group">
          <Lock className="icon" />
          <input
            type="password"
            placeholder="Password"
            value={form.password}
            onChange={(e) => setForm({ ...form, password: e.target.value })}
            required
          />
        </div>

        <button className="auth-button" type="submit" disabled={loading}>
          {loading ? "Logging in..." : "Login"}
        </button>
      </form>

      <p>
        Don't have an account? <Link to="/signup">Sign up</Link>
      </p>
    </div>
  );
};

export default LoginForm;
