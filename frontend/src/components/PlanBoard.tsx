import { Fragment, useEffect, useLayoutEffect, useRef, useState, type CSSProperties } from "react";
import { useTranslation } from "react-i18next";
import type { TimeEntry, WorkSchedule } from "../types";
import { formatDayLabel, hoursToHm } from "../utils/dateRange";
import { localToday, signedHm } from "../utils/hoursOverview";
import { initials } from "../utils/initials";
import { scheduledTimes } from "../utils/workSchedule";
import {
  buildRow,
  splitAtSchedule,
  stripRange,
  timeToHours,
  type DayItem,
  type HourRange,
  type SpanItem
} from "../utils/planBoard";
import { Icon } from "./Icon";

interface Props {
  days: string[];
  entries: TimeEntry[];
  onEdit?: (entry: TimeEntry) => void;
  /** Entries in a locked month can't be opened for editing. */
  isLocked?: (entry: TimeEntry) => boolean;
  /** Public holidays and company days off in view, as date → name. */
  holidays?: Record<string, string>;
  /**
   * Work schedules by user id, for people whose hours are compared with one. Their days show
   * the scheduled window and how far they are from it.
   */
  schedules?: Record<string, WorkSchedule>;
  /** Hour types booked as leave: days in a row of the same one are drawn as a single bar. */
  leaveHourTypeIds?: ReadonlySet<string>;
}

/** Differences smaller than this (in hours) aren't worth pointing out. */
const TOLERANCE = 0.25;
const NO_LEAVE: ReadonlySet<string> = new Set();

const hm = (hours: number) => `${String(Math.floor(hours)).padStart(2, "0")}:${String(Math.round((hours % 1) * 60)).padStart(2, "0")}`;
const isWeekend = (iso: string) => [0, 6].includes(new Date(iso + "T00:00:00").getDay());

/**
 * Week-at-a-glance planboard: one row per person, one column per day. Each day shows its total
 * and a time strip with a piece per entry, so breaks and split-up days stand out. With a work
 * schedule, the strip also shows the scheduled hours, hatches work outside them and gives the
 * difference with them. Days in a row of the same leave become one bar.
 */
export function PlanBoard({
  days: allDays,
  entries,
  onEdit,
  isLocked,
  holidays = {},
  schedules = {},
  leaveHourTypeIds = NO_LEAVE
}: Props) {
  const { t, i18n } = useTranslation();
  const lang = i18n.language.split("-")[0];
  const typeName = (e: TimeEntry) => e.localizedHourTypeNames?.[lang] || e.localizedHourTypeNames?.["en"] || "Unknown";
  const [open, setOpen] = useState<string | null>(null);

  // A weekend day nobody works on or is scheduled for only takes up room.
  const scheduleList = Object.values(schedules);
  const days = allDays.filter(
    (day) =>
      entries.some((e) => e.workDate === day) ||
      (scheduleList.length > 0 ? scheduleList.some((s) => scheduledTimes(s, day)) : !isWeekend(day))
  );

  const people = Array.from(new Map(entries.map((e) => [e.userId, e.userFullName])))
    .sort(([, a], [, b]) => a.localeCompare(b));
  const rows = people.map(([userId, name]) => ({
    userId,
    name,
    ...buildRow({
      days,
      entries: entries.filter((e) => e.userId === userId),
      schedule: schedules[userId] ?? null,
      holidays,
      leaveHourTypeIds
    })
  }));

  const windows = rows.flatMap((r) => r.items.flatMap((i) => (i.kind === "day" && i.window ? [i.window] : [])));
  const range = stripRange(entries, windows);
  const today = localToday();
  const hasSchedules = rows.some((r) => r.expectedHours !== null);

  const hasHolidays = days.some((day) => holidays[day]);
  const empty = <p className="empty-state">{t("planBoard.emptyState")}</p>;
  if (rows.length === 0 && !hasHolidays) return empty;

  const hourTypes = Array.from(new Map(entries.map((e) => [e.hourTypeId, e])).values());

  function handleClick(key: string, cellEntries: TimeEntry[]) {
    const only = cellEntries.length === 1 ? cellEntries[0] : null;
    if (only && onEdit && !isLocked?.(only)) {
      onEdit(only);
      return;
    }
    setOpen((current) => (current === key ? null : key));
  }

  function pick(entry: TimeEntry) {
    setOpen(null);
    onEdit?.(entry);
  }

  return (
    <div className="planboard">
      <div className="planboard__grid" style={{ gridTemplateColumns: `190px repeat(${days.length}, minmax(0, 1fr))` }}>
        <div className="planboard__corner" />
        {days.map((day) => (
          <div
            key={day}
            className={[
              "planboard__day-header",
              holidays[day] && "planboard__day-header--holiday",
              day === today && "planboard__day-header--today"
            ].filter(Boolean).join(" ")}
          >
            {formatDayLabel(day)}
            {holidays[day] && (
              <span className="planboard__holiday" title={holidays[day]}>
                {holidays[day]}
              </span>
            )}
          </div>
        ))}

        {rows.map((row) => (
          <Fragment key={row.userId}>
            <div className="planboard__row-label">
              <span className="planboard__avatar" aria-hidden="true">{initials(row.name)}</span>
              <span className="planboard__person">
                <span className="planboard__name">{row.name}</span>
                <span className="planboard__week">
                  {row.expectedHours !== null
                    ? `${hoursToHm(row.workedHours)} / ${hoursToHm(row.expectedHours)}`
                    : hoursToHm(row.workedHours)}
                </span>
                {row.expectedHours !== null && row.expectedHours > 0 && (
                  <span className="planboard__progress" aria-hidden="true">
                    <span style={{ width: `${Math.min(100, (row.workedHours / row.expectedHours) * 100)}%` }} />
                  </span>
                )}
              </span>
            </div>

            {row.items.map((item) => {
              const key = `${row.userId}-${item.kind === "day" ? item.date : item.days[0]}`;
              return item.kind === "span" ? (
                <Span
                  key={key}
                  item={item}
                  label={`${typeName(item.entries[0])} · ${formatDayLabel(item.days[0])} – ${formatDayLabel(item.days[item.days.length - 1])}`}
                  personName={row.name}
                  color={item.entries[0].hourTypeColor}
                  isOpen={open === key}
                  onClick={() => handleClick(key, item.entries)}
                  onClose={() => setOpen(null)}
                  list={<EntryList entries={item.entries} typeName={typeName} isLocked={isLocked} onPick={onEdit ? pick : undefined} showDates />}
                />
              ) : (
                <Day
                  key={key}
                  item={item}
                  personName={row.name}
                  hasSchedule={row.expectedHours !== null}
                  range={range}
                  holiday={!!holidays[item.date]}
                  isToday={item.date === today}
                  isPast={item.date < today}
                  typeName={typeName}
                  isOpen={open === key}
                  onClick={() => handleClick(key, item.entries)}
                  onClose={() => setOpen(null)}
                  list={<EntryList entries={item.entries} typeName={typeName} isLocked={isLocked} onPick={onEdit ? pick : undefined} />}
                />
              );
            })}
          </Fragment>
        ))}
      </div>

      {rows.length === 0 && empty}

      {rows.length > 0 && (
        <div className="planboard__legend">
          {hourTypes.map((e) => (
            <span key={e.hourTypeId} className="planboard__legend-item">
              <i className="planboard__swatch" style={{ background: e.hourTypeColor }} />
              {typeName(e)}
            </span>
          ))}
          {hasSchedules && (
            <>
              <span className="planboard__legend-item planboard__legend-item--push">
                <i className="planboard__swatch planboard__swatch--window" />
                {t("planBoard.legendSchedule")}
              </span>
              <span className="planboard__legend-item">
                <i className="planboard__swatch planboard__swatch--outside" />
                {t("planBoard.legendOutside")}
              </span>
            </>
          )}
          <span className={hasSchedules ? "planboard__legend-item" : "planboard__legend-item planboard__legend-item--push"}>
            {t("planBoard.legendRange", { from: hm(range.start), to: hm(range.end) })}
          </span>
        </div>
      )}
    </div>
  );
}

function Day({
  item,
  personName,
  hasSchedule,
  range,
  holiday,
  isToday,
  isPast,
  typeName,
  isOpen,
  onClick,
  onClose,
  list
}: {
  item: DayItem;
  personName: string;
  hasSchedule: boolean;
  range: HourRange;
  holiday: boolean;
  isToday: boolean;
  isPast: boolean;
  typeName: (e: TimeEntry) => string;
  isOpen: boolean;
  onClick: () => void;
  onClose: () => void;
  list: JSX.Element;
}) {
  const className = [
    "planboard__cell",
    holiday && "planboard__cell--holiday",
    isToday && "planboard__cell--today"
  ].filter(Boolean).join(" ");

  // Missing hours on a past working day are worth seeing; future days aren't behind yet.
  const difference =
    item.expectedHours !== null && (item.entries.length > 0 || isPast) ? item.workedHours - item.expectedHours : 0;
  const diff = Math.abs(difference) >= TOLERANCE && (
    <span className={difference < 0 ? "planboard__diff planboard__diff--short" : "planboard__diff planboard__diff--over"}>
      {signedHm(difference)}
    </span>
  );

  if (item.entries.length === 0) {
    return <div className={className}>{diff && <span className="planboard__day-top">{diff}</span>}</div>;
  }

  const width = range.end - range.start;
  const pos = (h: number) => `${((h - range.start) / width) * 100}%`;
  const size = (from: number, to: number) => `${((to - from) / width) * 100}%`;

  return (
    <div className={className}>
      <button
        type="button"
        className="planboard__day"
        aria-label={`${personName}, ${formatDayLabel(item.date)}, ${hoursToHm(item.workedHours)}`}
        aria-expanded={item.entries.length > 1 ? isOpen : undefined}
        title={item.entries.map((e) => `${typeName(e)} ${e.startTime.slice(0, 5)}–${e.endTime.slice(0, 5)}`).join("\n")}
        onClick={onClick}
      >
        <span className="planboard__day-top">
          <span className="planboard__total">{hoursToHm(item.workedHours)}</span>
          {diff}
        </span>
        <span className="planboard__strip" aria-hidden="true">
          {item.window && (
            <i className="planboard__window" style={{ left: pos(item.window.start), width: size(item.window.start, item.window.end) }} />
          )}
          {item.entries.flatMap((e) =>
            splitAtSchedule(timeToHours(e.startTime), timeToHours(e.endTime), item.window, hasSchedule).map((part) => (
              <i
                key={`${e.id}-${part.start}`}
                className={part.outside ? "planboard__seg planboard__seg--outside" : "planboard__seg"}
                style={{ left: pos(part.start), width: size(part.start, part.end), "--seg-color": e.hourTypeColor } as CSSProperties}
              />
            ))
          )}
        </span>
      </button>
      {isOpen && <Popover onClose={onClose}>{list}</Popover>}
    </div>
  );
}

function Span({
  item,
  label,
  personName,
  color,
  isOpen,
  onClick,
  onClose,
  list
}: {
  item: SpanItem;
  label: string;
  personName: string;
  color: string;
  isOpen: boolean;
  onClick: () => void;
  onClose: () => void;
  list: JSX.Element;
}) {
  return (
    <div className="planboard__cell planboard__span-cell" style={{ gridColumn: `span ${item.days.length}` }}>
      <button
        type="button"
        className="planboard__span"
        style={{ "--seg-color": color } as CSSProperties}
        aria-label={`${personName}, ${label}, ${hoursToHm(item.workedHours)}`}
        aria-expanded={isOpen}
        onClick={onClick}
      >
        <Icon name="leave" size={16} />
        <span className="planboard__span-label">{label}</span>
        <span className="planboard__span-total">{hoursToHm(item.workedHours)}</span>
      </button>
      {isOpen && <Popover onClose={onClose}>{list}</Popover>}
    </div>
  );
}

/**
 * The entries behind a cell, closing on Escape, a click elsewhere, or scrolling. It's placed
 * against the window under its cell, so the board's horizontal scrolling doesn't cut it off.
 */
function Popover({ onClose, children }: { onClose: () => void; children: JSX.Element }) {
  const ref = useRef<HTMLDivElement>(null);
  const [position, setPosition] = useState<CSSProperties>({ visibility: "hidden" });

  useLayoutEffect(() => {
    const cell = ref.current?.parentElement;
    const popover = ref.current;
    if (!cell || !popover) return;
    const rect = cell.getBoundingClientRect();
    const margin = 8;
    const left = Math.max(margin, Math.min(rect.left, window.innerWidth - popover.offsetWidth - margin));
    const below = rect.bottom + 4;
    const top = below + popover.offsetHeight > window.innerHeight - margin
      ? Math.max(margin, rect.top - popover.offsetHeight - 4)
      : below;
    setPosition({ top, left });
  }, []);

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => e.key === "Escape" && onClose();
    const onDown = (e: MouseEvent) => {
      const cell = ref.current?.parentElement;
      if (cell && !cell.contains(e.target as Node)) onClose();
    };
    const onMove = (e: Event) => {
      if (!ref.current?.contains(e.target as Node)) onClose();
    };
    document.addEventListener("keydown", onKey);
    document.addEventListener("mousedown", onDown);
    window.addEventListener("scroll", onMove, true);
    window.addEventListener("resize", onMove);
    return () => {
      document.removeEventListener("keydown", onKey);
      document.removeEventListener("mousedown", onDown);
      window.removeEventListener("scroll", onMove, true);
      window.removeEventListener("resize", onMove);
    };
  }, [onClose]);

  return (
    <div className="planboard__popover" role="dialog" ref={ref} style={position}>
      {children}
    </div>
  );
}

function EntryList({
  entries,
  typeName,
  isLocked,
  onPick,
  showDates = false
}: {
  entries: TimeEntry[];
  typeName: (e: TimeEntry) => string;
  isLocked?: (entry: TimeEntry) => boolean;
  onPick?: (entry: TimeEntry) => void;
  showDates?: boolean;
}) {
  const { t } = useTranslation();
  const total = entries.reduce((sum, e) => sum + e.durationHours, 0);
  return (
    <>
      <ul className="planboard__entries">
        {entries.map((e) => {
          const locked = isLocked?.(e) ?? false;
          const content = (
            <>
              <i className="planboard__swatch" style={{ background: e.hourTypeColor }} />
              <span className="planboard__entry-type">
                {typeName(e)}
                {e.projectName && <span className="planboard__entry-meta"> · {e.projectName}</span>}
              </span>
              <span className="planboard__entry-meta">
                {showDates && `${formatDayLabel(e.workDate)} `}
                {e.startTime.slice(0, 5)}–{e.endTime.slice(0, 5)}
              </span>
              {locked && <Icon name="lock" size={14} />}
            </>
          );
          return (
            <li key={e.id}>
              {onPick && !locked ? (
                <button type="button" className="planboard__entry" onClick={() => onPick(e)} title={e.notes ?? undefined}>
                  {content}
                </button>
              ) : (
                <div className="planboard__entry" title={locked ? t("lockedMonths.lockedHint") : e.notes ?? undefined}>
                  {content}
                </div>
              )}
            </li>
          );
        })}
      </ul>
      <div className="planboard__entries-total">
        <span>{t("planBoard.total")}</span>
        <span>{hoursToHm(total)}</span>
      </div>
    </>
  );
}
