import { FormEvent, useCallback, useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import {
  activateHourType,
  createHourType,
  deactivateHourType,
  getHourTypes,
  updateHourType
} from "../api/hourTypesApi";
import { extractErrorMessage } from "../api/client";
import type { HourType } from "../types";

const DEFAULT_COLOR = "#932e4a";

export function HourTypesPage() {
  const { t } = useTranslation();
  const [types, setTypes] = useState<HourType[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [name, setName] = useState("");
  const [color, setColor] = useState(DEFAULT_COLOR);
  const [creating, setCreating] = useState(false);

  const [editingId, setEditingId] = useState<string | null>(null);
  const [editName, setEditName] = useState("");
  const [editColor, setEditColor] = useState(DEFAULT_COLOR);
  const [savingEdit, setSavingEdit] = useState(false);

  const [busyId, setBusyId] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await getHourTypes(true);
      setTypes(data);
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setCreating(true);
    try {
      await createHourType(name, color);
      setName("");
      setColor(DEFAULT_COLOR);
      await load();
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setCreating(false);
    }
  }

  function startEdit(type: HourType) {
    setEditingId(type.id);
    setEditName(type.name);
    setEditColor(type.colorHex);
  }

  async function handleSaveEdit(e: FormEvent) {
    e.preventDefault();
    if (!editingId) return;
    setError(null);
    setSavingEdit(true);
    try {
      await updateHourType(editingId, editName, editColor);
      setEditingId(null);
      await load();
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setSavingEdit(false);
    }
  }

  async function handleToggleActive(type: HourType) {
    setBusyId(type.id);
    setError(null);
    try {
      if (type.isActive) {
        await deactivateHourType(type.id);
      } else {
        await activateHourType(type.id);
      }
      await load();
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setBusyId(null);
    }
  }

  return (
    <div className="page">
      <div className="page__header">
        <div>
          <h1>{t('hourTypes.title')}</h1>
          <p className="page__subtitle">
            {t('hourTypes.subtitle')}
          </p>
        </div>
      </div>

      <div className="panel">
        <h2>{t('hourTypes.addNewType')}</h2>
        <form className="form" onSubmit={handleCreate}>
          <div className="form__row">
            <label className="form__field">
              <span>{t('hourTypes.name')}</span>
              <input value={name} onChange={(e) => setName(e.target.value)} placeholder={t('hourTypes.namePlaceholder')} required />
            </label>
            <label className="form__field form__field--narrow">
              <span>{t('hourTypes.color')}</span>
              <input type="color" value={color} onChange={(e) => setColor(e.target.value)} />
            </label>
          </div>
          <div className="form__actions">
            <button className="btn btn--primary" type="submit" disabled={creating}>
              {creating ? t('hourTypes.adding') : t('hourTypes.addType')}
            </button>
          </div>
        </form>
      </div>

      {error && <p className="form__error">{error}</p>}

      {loading ? (
        <p className="empty-state">{t('common.loading')}</p>
      ) : (
        <table className="list-view">
          <thead>
            <tr>
              <th>{t('hourTypes.table.name')}</th>
              <th>{t('hourTypes.table.color')}</th>
              <th>{t('hourTypes.table.status')}</th>
              <th aria-label={t('hourTypes.table.actions')} />
            </tr>
          </thead>
          <tbody>
            {types.map((type) => (
              <tr key={type.id}>
                {editingId === type.id ? (
                  <td colSpan={4}>
                    <form className="form form--entry" onSubmit={handleSaveEdit}>
                      <div className="form__row">
                        <label className="form__field">
                          <span>{t('hourTypes.name')}</span>
                          <input value={editName} onChange={(e) => setEditName(e.target.value)} required />
                        </label>
                        <label className="form__field form__field--narrow">
                          <span>{t('hourTypes.color')}</span>
                          <input type="color" value={editColor} onChange={(e) => setEditColor(e.target.value)} />
                        </label>
                      </div>
                      <div className="form__actions">
                        <button className="btn btn--primary" type="submit" disabled={savingEdit}>
                          {savingEdit ? t('hourTypes.saving') : t('common.save')}
                        </button>
                        <button type="button" className="btn btn--ghost" onClick={() => setEditingId(null)}>
                          {t('common.cancel')}
                        </button>
                      </div>
                    </form>
                  </td>
                ) : (
                  <>
                    <td>{type.name}</td>
                    <td>
                      <span className="color-swatch" style={{ backgroundColor: type.colorHex }} />
                      <span className="list-view__notes">{type.colorHex}</span>
                    </td>
                    <td>
                      <span className={type.isActive ? "status status--active" : "status status--inactive"}>
                        {type.isActive ? t('hourTypes.statusActive') : t('hourTypes.statusDeactivated')}
                      </span>
                    </td>
                    <td className="list-view__actions">
                      <button className="btn btn--ghost btn--sm" onClick={() => startEdit(type)}>
                        {t('common.edit')}
                      </button>
                      <button
                        className={type.isActive ? "btn btn--ghost btn--sm btn--danger" : "btn btn--ghost btn--sm"}
                        onClick={() => handleToggleActive(type)}
                        disabled={busyId === type.id}
                      >
                        {type.isActive ? t('hourTypes.deactivate') : t('hourTypes.reactivate')}
                      </button>
                    </td>
                  </>
                )}
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}