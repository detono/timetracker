import { FormEvent, useState } from "react";
import { useTranslation } from "react-i18next";
import { changePassword } from "../api/authApi";
import { extractErrorMessage } from "../api/client";
import { useAuth } from "../auth/AuthContext";

export function AccountPage() {
  const { t } = useTranslation();
  const { user } = useAuth();
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

  // Safely translate the role if it exists
  const translatedRole = user?.role === "Employer" ? t('roles.employer') : t('roles.employee');

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