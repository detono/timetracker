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

interface LocalizedInput {
  lang: string;
  value: string;
}

export function HourTypesPage() {
  const { t, i18n } = useTranslation();
  const currentLang = i18n.language.split('-')[0];

  const [types, setTypes] = useState<HourType[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Creation state
  const [names, setNames] = useState<LocalizedInput[]>([
    { lang: "en", value: "" },
    { lang: "nl", value: "" },
    { lang: "fr", value: "" }
  ]);
  const [color, setColor] = useState(DEFAULT_COLOR);
  const [isDefault, setIsDefault] = useState(false);
  const [creating, setCreating] = useState(false);

  // Edit state
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editNames, setEditNames] = useState<LocalizedInput[]>([]);
  const [editColor, setEditColor] = useState(DEFAULT_COLOR);
  const [editIsDefault, setEditIsDefault] = useState(false);
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

  // Helper to convert our array of inputs into a clean Record<string, string>
  const buildDictionary = (inputs: LocalizedInput[]) => {
    return inputs.reduce((acc, curr) => {
      const lang = curr.lang.trim().toLowerCase();
      const val = curr.value.trim();
      if (lang && val) {
        acc[lang] = val;
      }
      return acc;
    }, {} as Record<string, string>);
  };

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setCreating(true);
    try {
      const dictionary = buildDictionary(names);
      if (Object.keys(dictionary).length === 0) {
        throw new Error("Please provide at least one valid language code and name.");
      }

      await createHourType(dictionary, color, isDefault);

      setNames([{ lang: "en", value: "" }, { lang: "nl", value: "" }, { lang: "fr", value: "" }]);
      setColor(DEFAULT_COLOR);
      setIsDefault(false);
      await load();
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setCreating(false);
    }
  }

  function startEdit(type: HourType) {
    setEditingId(type.id);

    // Map the dictionary back into an array of inputs
    const mappedNames = type.localizedNames
      ? Object.entries(type.localizedNames).map(([lang, value]) => ({ lang, value }))
      : [{ lang: "en", value: "" }];

    setEditNames(mappedNames);
    setEditColor(type.colorHex);
    setEditIsDefault(type.isDefault);
  }

  async function handleSaveEdit(e: FormEvent) {
    e.preventDefault();
    if (!editingId) return;
    setError(null);
    setSavingEdit(true);
    try {
      const dictionary = buildDictionary(editNames);
      if (Object.keys(dictionary).length === 0) {
        throw new Error("Please provide at least one valid language code and name.");
      }

      await updateHourType(editingId, dictionary, editColor, editIsDefault);
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

  const getDisplayName = (namesDict: Record<string, string> | undefined) => {
    if (!namesDict) return 'Unknown';
    return namesDict[currentLang] || namesDict['en'] || 'Unknown';
  };

  return (
    <div className="page">
      <div className="page__header">
        <div>
          <h1>{t('hourTypes.title', 'Hour Types')}</h1>
          <p className="page__subtitle">
            {t('hourTypes.subtitle', 'Manage categories for time entries')}
          </p>
        </div>
      </div>

      <div className="panel">
        <h2>{t('hourTypes.addNewType', 'Add New Type')}</h2>
        <form className="form" onSubmit={handleCreate}>

          {/* Dynamic Language Inputs */}
          <div className="form__row" style={{ flexWrap: 'wrap' }}>
            {names.map((item, idx) => (
              <div key={`create-lang-${idx}`} style={{ display: 'flex', gap: '8px', alignItems: 'flex-end', marginBottom: '8px' }}>
                <label className="form__field form__field--narrow">
                  <span>Code (e.g. en)</span>
                  <input
                    value={item.lang}
                    onChange={(e) => {
                      const newNames = [...names];
                      newNames[idx].lang = e.target.value;
                      setNames(newNames);
                    }}
                    placeholder="en"
                    required
                  />
                </label>
                <label className="form__field">
                  <span>Translation</span>
                  <input
                    value={item.value}
                    onChange={(e) => {
                      const newNames = [...names];
                      newNames[idx].value = e.target.value;
                      setNames(newNames);
                    }}
                    placeholder="e.g. Work"
                    required
                  />
                </label>
                {names.length > 1 && (
                  <button type="button" className="btn btn--ghost btn--sm btn--danger" style={{ marginBottom: '4px' }} onClick={() => setNames(names.filter((_, i) => i !== idx))}>
                    X
                  </button>
                )}
              </div>
            ))}

            <button
              type="button"
              className="btn btn--ghost btn--sm"
              style={{ alignSelf: 'center', marginTop: '16px' }}
              onClick={() => setNames([...names, { lang: '', value: '' }])}
            >
              + Add Language
            </button>
          </div>

          <div className="form__row" style={{ marginTop: '16px', alignItems: 'center' }}>
            <label className="form__field form__field--narrow">
              <span>{t('hourTypes.color', 'Color')}</span>
              <input type="color" value={color} onChange={(e) => setColor(e.target.value)} />
            </label>

            <label className="form__field" style={{ flexDirection: 'row', alignItems: 'center', gap: '8px', cursor: 'pointer' }}>
              <input
                type="checkbox"
                checked={isDefault}
                onChange={(e) => setIsDefault(e.target.checked)}
                style={{ width: 'auto' }}
              />
              <span>Set as Default</span>
            </label>
          </div>

          <div className="form__actions" style={{ marginTop: '16px' }}>
            <button className="btn btn--primary" type="submit" disabled={creating}>
              {creating ? t('hourTypes.adding', 'Adding...') : t('hourTypes.addType', 'Add Type')}
            </button>
          </div>
        </form>
      </div>

      {error && <p className="form__error">{error}</p>}

      {loading ? (
        <p className="empty-state">{t('common.loading', 'Loading...')}</p>
      ) : (
        <table className="list-view">
          <thead>
            <tr>
              <th>{t('hourTypes.table.name', 'Name')}</th>
              <th>{t('hourTypes.table.color', 'Color')}</th>
              <th>{t('hourTypes.table.status', 'Status')}</th>
              <th aria-label={t('hourTypes.table.actions', 'Actions')} />
            </tr>
          </thead>
          <tbody>
            {types.map((type) => (
              <tr key={type.id}>
                {editingId === type.id ? (
                  <td colSpan={4}>
                    <form className="form form--entry" onSubmit={handleSaveEdit}>

                      {/* Dynamic Language Inputs for Edit Mode */}
                      <div className="form__row" style={{ flexWrap: 'wrap' }}>
                        {editNames.map((item, idx) => (
                          <div key={`edit-lang-${idx}`} style={{ display: 'flex', gap: '8px', alignItems: 'flex-end', marginBottom: '8px' }}>
                            <label className="form__field form__field--narrow">
                              <span>Code</span>
                              <input
                                value={item.lang}
                                onChange={(e) => {
                                  const newNames = [...editNames];
                                  newNames[idx].lang = e.target.value;
                                  setEditNames(newNames);
                                }}
                                required
                              />
                            </label>
                            <label className="form__field">
                              <span>Translation</span>
                              <input
                                value={item.value}
                                onChange={(e) => {
                                  const newNames = [...editNames];
                                  newNames[idx].value = e.target.value;
                                  setEditNames(newNames);
                                }}
                                required
                              />
                            </label>
                            {editNames.length > 1 && (
                              <button type="button" className="btn btn--ghost btn--sm btn--danger" style={{ marginBottom: '4px' }} onClick={() => setEditNames(editNames.filter((_, i) => i !== idx))}>
                                X
                              </button>
                            )}
                          </div>
                        ))}
                        <button
                          type="button"
                          className="btn btn--ghost btn--sm"
                          style={{ alignSelf: 'center' }}
                          onClick={() => setEditNames([...editNames, { lang: '', value: '' }])}
                        >
                          + Add Language
                        </button>
                      </div>

                      <div className="form__row" style={{ marginTop: '16px', alignItems: 'center' }}>
                        <label className="form__field form__field--narrow">
                          <span>{t('hourTypes.color', 'Color')}</span>
                          <input type="color" value={editColor} onChange={(e) => setEditColor(e.target.value)} />
                        </label>

                        <label className="form__field" style={{ flexDirection: 'row', alignItems: 'center', gap: '8px', cursor: 'pointer' }}>
                          <input
                            type="checkbox"
                            checked={editIsDefault}
                            onChange={(e) => setEditIsDefault(e.target.checked)}
                            style={{ width: 'auto' }}
                          />
                          <span>Set as Default</span>
                        </label>
                      </div>

                      <div className="form__actions" style={{ marginTop: '16px' }}>
                        <button className="btn btn--primary" type="submit" disabled={savingEdit}>
                          {savingEdit ? t('hourTypes.saving', 'Saving...') : t('common.save', 'Save')}
                        </button>
                        <button type="button" className="btn btn--ghost" onClick={() => setEditingId(null)}>
                          {t('common.cancel', 'Cancel')}
                        </button>
                      </div>
                    </form>
                  </td>
                ) : (
                  <>
                    <td>
                      {getDisplayName(type.localizedNames)}
                      {type.isDefault && <span style={{ marginLeft: '8px', fontSize: '0.85em' }}>⭐</span>}
                    </td>
                    <td>
                      <span className="color-swatch" style={{ backgroundColor: type.colorHex }} />
                      <span className="list-view__notes">{type.colorHex}</span>
                    </td>
                    <td>
                      <span className={type.isActive ? "status status--active" : "status status--inactive"}>
                        {type.isActive ? t('hourTypes.statusActive', 'Active') : t('hourTypes.statusDeactivated', 'Deactivated')}
                      </span>
                    </td>
                    <td className="list-view__actions">
                      <button className="btn btn--ghost btn--sm" onClick={() => startEdit(type)}>
                        {t('common.edit', 'Edit')}
                      </button>
                      <button
                        className={type.isActive ? "btn btn--ghost btn--sm btn--danger" : "btn btn--ghost btn--sm"}
                        onClick={() => handleToggleActive(type)}
                        disabled={busyId === type.id}
                      >
                        {type.isActive ? t('hourTypes.deactivate', 'Deactivate') : t('hourTypes.reactivate', 'Reactivate')}
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