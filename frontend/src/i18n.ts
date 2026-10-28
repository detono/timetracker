import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import LanguageDetector from 'i18next-browser-languagedetector';

/**
 * Every file in ./locales is a language, named by its two-letter code (de.json = German).
 * Adding a language means adding one file; nothing needs registering here. Keys missing
 * from a language fall back to English one by one, so partial translations work.
 */
const files = import.meta.glob<Record<string, unknown>>('./locales/*.json', { eager: true, import: 'default' });

const resources = Object.fromEntries(
  Object.entries(files).map(([path, translation]) => [path.match(/([a-z]{2})\.json$/)![1], { translation }])
);

/** The languages there's a translation file for, English first. */
export const availableLanguages: string[] = Object.keys(resources).sort((a, b) =>
  a === 'en' ? -1 : b === 'en' ? 1 : a.localeCompare(b)
);

i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources,
    supportedLngs: availableLanguages,
    fallbackLng: 'en',
    detection: {
      // "nl-BE" → "nl": translations are per language, not per country.
      convertDetectedLanguage: (lng: string) => lng.split('-')[0].toLowerCase()
    },
    interpolation: {
      escapeValue: false // React already safely escapes HTML
    }
  });

export default i18n;
