import { Injectable } from '@angular/core';

export interface CompletionCardInput {
  title: string;
  kindLabel: string;
  statusLabel: string;
  coverUrl: string;
  rating?: number | null;
  completedAt?: Date | string | null;
  detailLines?: string[];
}

@Injectable({ providedIn: 'root' })
export class CompletionCardService {
  async export(input: CompletionCardInput): Promise<string> {
    const fileName = completionCardFileName(input.title);
    const cover = await loadImage(input.coverUrl).catch(() => null);
    const blob = await renderCardBlob(input, cover).catch(() => renderCardBlob(input, null));
    triggerDownload(blob, fileName);
    return fileName;
  }
}

export function completionCardFileName(title: string): string {
  const safeTitle = title
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
    .slice(0, 60);
  return `${safeTitle || 'completion'}-completion-card.png`;
}

async function renderCardBlob(input: CompletionCardInput, cover: HTMLImageElement | null): Promise<Blob> {
  const canvas = document.createElement('canvas');
  canvas.width = 1200;
  canvas.height = 630;
  const ctx = canvas.getContext('2d');
  if (!ctx) {
    throw new Error('Canvas is not available.');
  }

  drawCard(ctx, canvas.width, canvas.height, input, cover);
  return canvasToBlob(canvas);
}

function drawCard(
  ctx: CanvasRenderingContext2D,
  width: number,
  height: number,
  input: CompletionCardInput,
  cover: HTMLImageElement | null,
): void {
  const background = ctx.createLinearGradient(0, 0, width, height);
  background.addColorStop(0, '#07111f');
  background.addColorStop(0.58, '#10223a');
  background.addColorStop(1, '#0a2f27');
  ctx.fillStyle = background;
  ctx.fillRect(0, 0, width, height);

  ctx.fillStyle = 'rgba(96, 165, 250, 0.16)';
  ctx.beginPath();
  ctx.arc(1040, 90, 260, 0, Math.PI * 2);
  ctx.fill();

  ctx.fillStyle = 'rgba(34, 211, 238, 0.12)';
  ctx.beginPath();
  ctx.arc(120, 560, 260, 0, Math.PI * 2);
  ctx.fill();

  const coverBox = { x: 72, y: 86, w: 304, h: 456 };
  roundedRect(ctx, coverBox.x - 12, coverBox.y - 12, coverBox.w + 24, coverBox.h + 24, 34);
  ctx.fillStyle = 'rgba(255, 255, 255, 0.11)';
  ctx.fill();

  if (cover) {
    ctx.save();
    roundedRect(ctx, coverBox.x, coverBox.y, coverBox.w, coverBox.h, 26);
    ctx.clip();
    drawImageCover(ctx, cover, coverBox.x, coverBox.y, coverBox.w, coverBox.h);
    ctx.restore();
  } else {
    roundedRect(ctx, coverBox.x, coverBox.y, coverBox.w, coverBox.h, 26);
    ctx.fillStyle = 'rgba(255, 255, 255, 0.08)';
    ctx.fill();
    ctx.fillStyle = '#93c5fd';
    ctx.font = '700 34px Inter, Arial, sans-serif';
    ctx.textAlign = 'center';
    ctx.fillText(input.kindLabel, coverBox.x + coverBox.w / 2, coverBox.y + coverBox.h / 2);
    ctx.textAlign = 'left';
  }

  const contentX = 430;
  ctx.fillStyle = '#67e8f9';
  ctx.font = '800 28px Inter, Arial, sans-serif';
  ctx.fillText('LuminaPath completion', contentX, 112);

  ctx.fillStyle = '#f8fafc';
  ctx.font = '900 74px Inter, Arial, sans-serif';
  drawWrappedText(ctx, input.title, contentX, 178, 650, 82, 3);

  const statusY = 420;
  roundedRect(ctx, contentX, statusY - 44, 300, 58, 28);
  ctx.fillStyle = 'rgba(74, 222, 128, 0.18)';
  ctx.fill();
  ctx.fillStyle = '#bbf7d0';
  ctx.font = '800 25px Inter, Arial, sans-serif';
  ctx.fillText(input.statusLabel, contentX + 24, statusY - 7);

  if (input.rating != null) {
    roundedRect(ctx, contentX + 326, statusY - 44, 170, 58, 28);
    ctx.fillStyle = 'rgba(250, 204, 21, 0.18)';
    ctx.fill();
    ctx.fillStyle = '#fde68a';
    ctx.font = '800 25px Inter, Arial, sans-serif';
    ctx.fillText(`${input.rating}/10`, contentX + 350, statusY - 7);
  }

  const lines = [
    input.kindLabel,
    ...((input.detailLines ?? []).filter(Boolean)),
    completedLabel(input.completedAt),
  ].filter(Boolean);

  ctx.fillStyle = '#cbd5e1';
  ctx.font = '600 24px Inter, Arial, sans-serif';
  lines.slice(0, 4).forEach((line, index) => {
    ctx.fillText(line, contentX, 500 + index * 34);
  });

  ctx.fillStyle = 'rgba(255, 255, 255, 0.66)';
  ctx.font = '700 22px Inter, Arial, sans-serif';
  ctx.fillText('Game life tracker', 916, 558);
}

function completedLabel(value: Date | string | null | undefined): string {
  if (!value) {
    return '';
  }

  const date = value instanceof Date ? value : new Date(value);
  if (Number.isNaN(date.getTime())) {
    return '';
  }

  return `Completed ${new Intl.DateTimeFormat('en', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  }).format(date)}`;
}

function drawWrappedText(
  ctx: CanvasRenderingContext2D,
  text: string,
  x: number,
  y: number,
  maxWidth: number,
  lineHeight: number,
  maxLines: number,
): void {
  const words = text.trim().split(/\s+/);
  const lines: string[] = [];
  let current = '';

  for (const word of words) {
    const next = current ? `${current} ${word}` : word;
    if (ctx.measureText(next).width <= maxWidth || !current) {
      current = next;
      continue;
    }
    lines.push(current);
    current = word;
    if (lines.length === maxLines - 1) {
      break;
    }
  }

  if (current && lines.length < maxLines) {
    lines.push(current);
  }

  lines.forEach((line, index) => {
    const clipped = index === maxLines - 1 && words.join(' ').length > lines.join(' ').length
      ? `${line.replace(/[.,;:!?]$/, '')}...`
      : line;
    ctx.fillText(clipped, x, y + index * lineHeight);
  });
}

function drawImageCover(
  ctx: CanvasRenderingContext2D,
  image: HTMLImageElement,
  x: number,
  y: number,
  width: number,
  height: number,
): void {
  const scale = Math.max(width / image.naturalWidth, height / image.naturalHeight);
  const drawWidth = image.naturalWidth * scale;
  const drawHeight = image.naturalHeight * scale;
  ctx.drawImage(
    image,
    x + (width - drawWidth) / 2,
    y + (height - drawHeight) / 2,
    drawWidth,
    drawHeight,
  );
}

function roundedRect(
  ctx: CanvasRenderingContext2D,
  x: number,
  y: number,
  width: number,
  height: number,
  radius: number,
): void {
  ctx.beginPath();
  ctx.moveTo(x + radius, y);
  ctx.arcTo(x + width, y, x + width, y + height, radius);
  ctx.arcTo(x + width, y + height, x, y + height, radius);
  ctx.arcTo(x, y + height, x, y, radius);
  ctx.arcTo(x, y, x + width, y, radius);
  ctx.closePath();
}

function loadImage(src: string): Promise<HTMLImageElement> {
  return new Promise((resolve, reject) => {
    if (!src) {
      reject(new Error('Missing image source.'));
      return;
    }

    const image = new Image();
    image.crossOrigin = 'anonymous';
    image.onload = () => resolve(image);
    image.onerror = () => reject(new Error('Image could not be loaded.'));
    image.src = src;
  });
}

function canvasToBlob(canvas: HTMLCanvasElement): Promise<Blob> {
  return new Promise((resolve, reject) => {
    canvas.toBlob((blob) => {
      if (blob) {
        resolve(blob);
        return;
      }
      reject(new Error('Card could not be exported.'));
    }, 'image/png');
  });
}

function triggerDownload(blob: Blob, fileName: string): void {
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = fileName;
  anchor.rel = 'noopener';
  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();
  setTimeout(() => URL.revokeObjectURL(url), 0);
}
