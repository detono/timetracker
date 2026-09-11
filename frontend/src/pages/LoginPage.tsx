import { FormEvent, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { login } from "../api/authApi";
import { extractErrorMessage } from "../api/client";
import { useAuth } from "../auth/AuthContext";
import { appConfig } from "../config";

export function LoginPage() {
  const { t } = useTranslation();
  const auth = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      const result = await login(email, password);
      auth.login(result);
      navigate("/");
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="auth-page">
      <div className="auth-card">
        <div className="auth-card__brand">{appConfig.title}</div>
        <p className="auth-card__tagline">{t('login.tagline')}</p>

        <form onSubmit={handleSubmit} className="form">
          <label className="form__field">
            <span>{t('login.email')}</span>
            <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required autoFocus />
          </label>
          <label className="form__field">
            <span>{t('login.password')}</span>
            <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} required />
          </label>

          {error && <p className="form__error">{error}</p>}

          <button className="btn btn--primary" type="submit" disabled={loading}>
            {loading ? t('common.signingIn') : t('login.signIn')}
          </button>
        </form>
      </div>
    </div>
  );
}