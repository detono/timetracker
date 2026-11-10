import type { ReactNode } from "react";

// `code`, **bold**, *italic* and [text](https://…), the bits of Markdown the changelog uses.
const pattern = /`([^`]+)`|\*\*(.+?)\*\*|\*([^*\s][^*]*?)\*|\[([^\]]+)\]\((https?:\/\/[^)\s]+)\)/g;

/**
 * Shows a line of simple Markdown as text, without using innerHTML: anything it doesn't
 * recognise stays plain text.
 */
export function InlineMarkdown({ text }: { text: string }) {
  return <>{render(text)}</>;
}

function render(text: string): ReactNode[] {
  const nodes: ReactNode[] = [];
  let last = 0;
  for (const match of text.matchAll(pattern)) {
    const index = match.index ?? 0;
    if (index > last) nodes.push(text.slice(last, index));
    const [, code, bold, italic, linkText, href] = match;
    const key = nodes.length;
    if (code !== undefined) nodes.push(<code key={key}>{code}</code>);
    else if (bold !== undefined) nodes.push(<strong key={key}>{render(bold)}</strong>);
    else if (italic !== undefined) nodes.push(<em key={key}>{render(italic)}</em>);
    else
      nodes.push(
        <a key={key} href={href} target="_blank" rel="noopener noreferrer">
          {render(linkText)}
        </a>
      );
    last = index + match[0].length;
  }
  if (last < text.length) nodes.push(text.slice(last));
  return nodes;
}
