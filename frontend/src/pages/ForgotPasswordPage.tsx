import { FormEvent, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { confirmPasswordReset, login, requestPasswordReset } from "../api/authApi";
import { extractErrorMessage } from "../api/client";
import { useAuth } from "../auth/AuthContext";
import { appConfig } from "../config";

/**
 * Two steps on one page: ask for a code by email, then enter it with a new password. The
 * code (not a link) keeps people on this page and works however the app is hosted.
 */
export function ForgotPasswordPage() {
  const { t } = useTranslation();
  const auth = useAuth();
  const navigate = useNavigate();
  const [step, setStep] = useState<"email" | "code">("email");
  const [email, setEmail] = useState("");
  const [code, setCode] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function handleRequest(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      await requestPasswordReset(email.trim());
      setStep("code");
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  async function handleReset(e: FormEvent) {
    e.preventDefault();
    setError(null);
    if (newPassword !== confirmPassword) {
      setError(t('account.errorMismatch'));
      return;
    }

    setBusy(true);
    try {
      await confirmPasswordReset(email.trim(), code, newPassword);
      auth.login(await login(email.trim(), newPassword));
      navigate("/");
    } catch (err) {
      setError(extractErrorMessage(err));
      setBusy(false);
    }
  }

  return (
    <div className="auth-page">
      <div className="auth-card">
        <div className="auth-card__brand">{appConfig.title}</div>
        <h1 className="auth-card__title">{t('forgotPassword.title')}</h1>

        {step === "email" ? (
          <form onSubmit={handleRequest} className="form">
            <p className="auth-card__tagline">{t('forgotPassword.intro')}</p>
            <label className="form__field">
              <span>{t('login.email')}</span>
              <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required autoFocus />
            </label>

            {error && <p className="form__error">{error}</p>}

            <button className="btn btn--primary" type="submit" disabled={busy}>
              {busy ? t('forgotPassword.sending') : t('forgotPassword.sendCode')}
            </button>
          </form>
        ) : (
          <form onSubmit={handleReset} className="form">
            <p className="auth-card__tagline">{t('forgotPassword.sent', { email: email.trim() })}</p>
            <label className="form__field">
              <span>{t('forgotPassword.code')}</span>
              <input
                value={code}
                onChange={(e) => setCode(e.target.value)}
                placeholder="ABCD-EFGH"
                autoComplete="one-time-code"
                autoCapitalize="characters"
                spellCheck={false}
                maxLength={32}
                required
                autoFocus
              />
            </label>
            <label className="form__field">
              <span>{t('account.newPassword')}</span>
              <input
                type="password"
                autoComplete="new-password"
                value={newPassword}
                onChange={(e) => setNewPassword(e.target.value)}
                minLength={8}
                placeholder={t('employees.min8chars')}
                required
              />
            </label>
            <label className="form__field">
              <span>{t('account.confirmPassword')}</span>
              <input
                type="password"
                autoComplete="new-password"
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                minLength={8}
                required
              />
            </label>

            {error && <p className="form__error">{error}</p>}

            <button className="btn btn--primary" type="submit" disabled={busy}>
              {busy ? t('common.saving') : t('forgotPassword.setPassword')}
            </button>

            <p className="form__hint">{t('forgotPassword.noEmail')}</p>
            <button
              type="button"
              className="link-button"
              onClick={() => {
                setStep("email");
                setCode("");
                setError(null);
              }}
            >
              {t('forgotPassword.sendNewCode')}
            </button>
          </form>
        )}

        <p className="auth-card__links">
          <Link to="/login">{t('forgotPassword.backToSignIn')}</Link>
        </p>
      </div>
    </div>
  );
}
