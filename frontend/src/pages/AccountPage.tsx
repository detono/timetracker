import { FormEvent, useState } from "react";
import { useTranslation } from "react-i18next";
import { changePassword, setLanguage } from "../api/authApi";
import { availableLanguages } from "../i18n";
import { nativeLanguageName } from "../utils/languages";
import { extractErrorMessage } from "../api/client";
import { useAuth } from "../auth/AuthContext";
import { roleLabel } from "../utils/roles";

export function AccountPage() {
  const { t, i18n } = useTranslation();
  const { user } = useAuth();
  const [languageError, setLanguageError] = useState<string | null>(null);

  async function handleLanguageChange(language: string) {
    setLanguageError(null);
    await i18n.changeLanguage(language);
    try {
      await setLanguage(language);
    } catch (err) {
      setLanguageError(extractErrorMessage(err));
    }
  }
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setSuccess(false);

    if (newPassword !== confirmPassword) {
      setError(t('account.errorMismatch'));
      return;
    }

    setSubmitting(true);
    try {
      await changePassword(currentPassword, newPassword);
      setSuccess(true);
      setCurrentPassword("");
      setNewPassword("");
      setConfirmPassword("");
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setSubmitting(false);
    }
  }

  const translatedRole = user ? roleLabel(t, user.role) : "";

  return (
    <div className="page">
      <div className="page__header">
        <div>
          <h1>{t('account.title')}</h1>
          <p className="page__subtitle">
            {t('account.signedInAs')} <strong>{user?.fullName}</strong> ({translatedRole})
          </p>
        </div>
      </div>

      <div className="panel" style={{ maxWidth: 420 }}>
        <h2>{t('account.languageTitle')}</h2>
        <label className="form__field">
          <span>{t('account.languageLabel')}</span>
          <select value={i18n.resolvedLanguage ?? "en"} onChange={(e) => handleLanguageChange(e.target.value)}>
            {availableLanguages.map((code) => (
              <option key={code} value={code}>
                {nativeLanguageName(code)}
              </option>
            ))}
          </select>
        </label>
        <p className="form__hint">{t('account.languageHint')}</p>
        {languageError && <p className="form__error">{languageError}</p>}
      </div>

      <div className="panel" style={{ maxWidth: 420 }}>
        <h2>{t('account.changePasswordTitle')}</h2>
        <form className="form" onSubmit={handleSubmit}>
          <label className="form__field">
            <span>{t('account.currentPassword')}</span>
            <input
              type="password"
              value={currentPassword}
              onChange={(e) => setCurrentPassword(e.target.value)}
              required
            />
          </label>
          <label className="form__field">
            <span>{t('account.newPassword')}</span>
            <input
              type="password"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              minLength={8}
              required
            />
          </label>
          <label className="form__field">
            <span>{t('account.confirmPassword')}</span>
            <input
              type="password"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              minLength={8}
              required
            />
          </label>

          {error && <p className="form__error">{error}</p>}
          {success && <p className="form__success">{t('account.successMessage')}</p>}

          <div className="form__actions">
            <button className="btn btn--primary" type="submit" disabled={submitting}>
              {submitting ? t('account.updating') : t('account.updateBtn')}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}