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
import { RowMenu } from "../components/RowMenu";

const DEFAULT_COLOR = "#932e4a";

interface LeaveOptions {
  isLeave: boolean;
  deductsFromLeaveBalance: boolean;
}

const noLeave: LeaveOptions = { isLeave: false, deductsFromLeaveBalance: false };

/** The two leave checkboxes; "uses up the balance" only applies to leave types. */
function LeaveOptionsFields({ value, onChange }: { value: LeaveOptions; onChange: (value: LeaveOptions) => void }) {
  const { t } = useTranslation();
  return (
    <div className="form__row form__row--options">
      <label className="check-field">
        <input
          type="checkbox"
          checked={value.isLeave}
          onChange={(e) => onChange({ isLeave: e.target.checked, deductsFromLeaveBalance: e.target.checked && value.deductsFromLeaveBalance })}
        />
        <span>{t('hourTypes.isLeave')}</span>
      </label>
      <label className="check-field">
        <input
          type="checkbox"
          checked={value.deductsFromLeaveBalance}
          disabled={!value.isLeave}
          onChange={(e) => onChange({ ...value, deductsFromLeaveBalance: e.target.checked })}
        />
        <span>{t('hourTypes.deductsFromBalance')}</span>
      </label>
    </div>
  );
}

interface LocalizedInput {
  lang: string;
  value: string;
}

/** The hour type's name per language: one row per language code, with add and remove. */
function NameTranslationsField({ rows, onChange }: { rows: LocalizedInput[]; onChange: (rows: LocalizedInput[]) => void }) {
  const { t } = useTranslation();
  const update = (idx: number, patch: Partial<LocalizedInput>) =>
    onChange(rows.map((row, i) => (i === idx ? { ...row, ...patch } : row)));

  return (
    <fieldset className="translation-rows">
      <legend>{t('hourTypes.namesLegend')}</legend>
      <div className="translation-row translation-row--head" aria-hidden="true">
        <span>{t('hourTypes.languageCode')}</span>
        <span>{t('hourTypes.name')}</span>
      </div>
      {rows.map((row, idx) => (
        <div key={idx} className="translation-row">
          <input
            value={row.lang}
            onChange={(e) => update(idx, { lang: e.target.value })}
            placeholder="en"
            aria-label={t('hourTypes.languageCode')}
            maxLength={5}
            required
          />
          <input
            value={row.value}
            onChange={(e) => update(idx, { value: e.target.value })}
            placeholder={t('hourTypes.namePlaceholder')}
            aria-label={t('hourTypes.name')}
            required
          />
          {rows.length > 1 ? (
            <button
              type="button"
              className="icon-btn"
              onClick={() => onChange(rows.filter((_, i) => i !== idx))}
              aria-label={t('hourTypes.removeLanguage', { code: row.lang || '?' })}
              title={t('hourTypes.removeLanguage', { code: row.lang || '?' })}
            >
              ×
            </button>
          ) : (
            <span />
          )}
        </div>
      ))}
      <button type="button" className="btn btn--ghost btn--sm" onClick={() => onChange([...rows, { lang: '', value: '' }])}>
        + {t('hourTypes.addLanguage')}
      </button>
    </fieldset>
  );
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
  const [leaveOptions, setLeaveOptions] = useState(noLeave);
  const [creating, setCreating] = useState(false);

  // Edit state
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editNames, setEditNames] = useState<LocalizedInput[]>([]);
  const [editColor, setEditColor] = useState(DEFAULT_COLOR);
  const [editIsDefault, setEditIsDefault] = useState(false);
  const [editLeaveOptions, setEditLeaveOptions] = useState(noLeave);
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

      await createHourType({ localizedNames: dictionary, colorHex: color, isDefault, ...leaveOptions });

      setNames([{ lang: "en", value: "" }, { lang: "nl", value: "" }, { lang: "fr", value: "" }]);
      setColor(DEFAULT_COLOR);
      setIsDefault(false);
      setLeaveOptions(noLeave);
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
    setEditLeaveOptions({ isLeave: type.isLeave, deductsFromLeaveBalance: type.deductsFromLeaveBalance });
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

      await updateHourType(editingId, {
        localizedNames: dictionary,
        colorHex: editColor,
        isDefault: editIsDefault,
        ...editLeaveOptions
      });
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

  const getValidHex = (hex: string | undefined) => {
    if (!hex) return '#000000'; // Fallback to black if null/empty
    if (/^#[0-9A-Fa-f]{6}$/.test(hex)) return hex; // Perfect format
    if (/^#[0-9A-Fa-f]{3}$/.test(hex)) {
      // Expand #fff to #ffffff
      return '#' + hex[1] + hex[1] + hex[2] + hex[2] + hex[3] + hex[3];
    }
    if (/^[0-9A-Fa-f]{6}$/.test(hex)) return '#' + hex; // Add missing hash

    return '#000000'; // Ultimate fallback
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

          <NameTranslationsField rows={names} onChange={setNames} />

          <div className="form__row form__row--options">
            <label className="form__field form__field--narrow">
              <span>{t('hourTypes.color', 'Color')}</span>

              <input
                type="color"
                value={getValidHex(color)}
                onChange={(e) => setColor(e.target.value)}
              />
            </label>

            <label className="check-field">
              <input
                type="checkbox"
                checked={isDefault}
                onChange={(e) => setIsDefault(e.target.checked)}
              />
              <span>{t('hourTypes.setAsDefault')}</span>
            </label>
          </div>

          <LeaveOptionsFields value={leaveOptions} onChange={setLeaveOptions} />

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
        <div className="table-scroll">
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

                        <NameTranslationsField rows={editNames} onChange={setEditNames} />

                        <div className="form__row form__row--options">
                          <label className="form__field form__field--narrow">
                            <span>{t('hourTypes.color', 'Color')}</span>
                            <input
                              type="color"
                              value={getValidHex(editColor)}
                              onChange={(e) => setEditColor(e.target.value)}
                            />
                          </label>

                          <label className="check-field">
                            <input
                              type="checkbox"
                              checked={editIsDefault}
                              onChange={(e) => setEditIsDefault(e.target.checked)}
                                          />
                            <span>{t('hourTypes.setAsDefault')}</span>
                          </label>
                        </div>

                        <LeaveOptionsFields value={editLeaveOptions} onChange={setEditLeaveOptions} />

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
                        {type.isLeave && (
                          <span className="badge" title={type.deductsFromLeaveBalance ? t('hourTypes.deductsFromBalance') : undefined}>
                            {type.deductsFromLeaveBalance ? t('hourTypes.leaveBadgeBalance') : t('hourTypes.leaveBadge')}
                          </span>
                        )}
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
                        <RowMenu
                          disabled={busyId === type.id}
                          items={[
                            { label: t('common.edit', 'Edit'), onSelect: () => startEdit(type) },
                            {
                              label: type.isActive ? t('hourTypes.deactivate', 'Deactivate') : t('hourTypes.reactivate', 'Reactivate'),
                              onSelect: () => handleToggleActive(type),
                              danger: type.isActive
                            }
                          ]}
                        />
                      </td>
                    </>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}