import { FormEvent, useState } from "react";
import { useTranslation } from "react-i18next";
import { changePassword } from "../api/authApi";
import { extractErrorMessage } from "../api/client";
import { useAuth } from "../auth/AuthContext";
import { appConfig } from "../config";

/** Shown instead of the app while someone still has a password that someone else chose. */
export function ChoosePasswordPage() {
  const { t } = useTranslation();
  const auth = useAuth();
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    if (newPassword !== confirmPassword) {
      setError(t('account.errorMismatch'));
      return;
    }

    setSaving(true);
    try {
      auth.login(await changePassword(null, newPassword));
    } catch (err) {
      setError(extractErrorMessage(err));
      setSaving(false);
    }
  }

  return (
    <div className="auth-page">
      <div className="auth-card">
        <div className="auth-card__brand">{appConfig.title}</div>
        <h1 className="auth-card__title">{t('choosePassword.title')}</h1>
        <p className="auth-card__tagline">
          {t('choosePassword.intro', { name: auth.user?.fullName ?? "" })}
        </p>

        <form onSubmit={handleSubmit} className="form">
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
              autoFocus
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

          <button className="btn btn--primary" type="submit" disabled={saving}>
            {saving ? t('common.saving') : t('choosePassword.submit')}
          </button>
        </form>

        <p className="auth-card__links">
          <button type="button" className="link-button" onClick={auth.logout}>
            {t('choosePassword.signOut')}
          </button>
        </p>
      </div>
    </div>
  );
}
