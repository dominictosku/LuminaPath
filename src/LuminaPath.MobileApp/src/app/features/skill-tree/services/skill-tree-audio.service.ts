import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class SkillTreeAudioService {
  private ctx: AudioContext | null = null;
  private masterGain: GainNode | null = null;
  private muted = false;

  init(): void {
    if (this.ctx) return;
    const AC = (window as any).AudioContext || (window as any).webkitAudioContext;
    if (!AC) return;
    this.ctx = new AC();
    this.masterGain = this.ctx!.createGain();
    this.masterGain.gain.value = 0.5;
    this.masterGain.connect(this.ctx!.destination);
  }

  setMuted(muted: boolean): void {
    this.muted = muted;
    if (this.masterGain) {
      this.masterGain.gain.value = muted ? 0 : 0.5;
    }
  }

  isMuted(): boolean {
    return this.muted;
  }

  unlock(): void {
    this.init();
    const base = 523.25;
    this.chord([base, base * 1.25, base * 1.5, base * 1.875], 1.4, 0.18, 'sine');
    setTimeout(() => this.tone(base * 2.5, 1.6, 0.14, 'sine'), 220);
    setTimeout(() => this.tone(base * 3, 2.0, 0.08, 'triangle'), 360);
  }

  hover(): void {
    this.init();
    this.tone(880, 0.18, 0.06, 'sine');
  }

  click(): void {
    this.init();
    this.tone(1200, 0.08, 0.08, 'triangle');
  }

  whoosh(): void {
    this.init();
    if (!this.ctx || this.muted || !this.masterGain) return;
    const ctx = this.ctx;
    const t0 = ctx.currentTime;
    const dur = 0.7;
    const buf = ctx.createBuffer(1, ctx.sampleRate * dur, ctx.sampleRate);
    const data = buf.getChannelData(0);
    for (let i = 0; i < data.length; i++) data[i] = (Math.random() * 2 - 1) * 0.6;
    const src = ctx.createBufferSource();
    src.buffer = buf;
    const filter = ctx.createBiquadFilter();
    filter.type = 'bandpass';
    filter.Q.value = 6;
    filter.frequency.setValueAtTime(200, t0);
    filter.frequency.exponentialRampToValueAtTime(2400, t0 + dur);
    const g = ctx.createGain();
    g.gain.setValueAtTime(0, t0);
    g.gain.linearRampToValueAtTime(0.18, t0 + 0.1);
    g.gain.exponentialRampToValueAtTime(0.0001, t0 + dur);
    src.connect(filter).connect(g).connect(this.masterGain);
    src.start(t0);
    src.stop(t0 + dur);
  }

  complete(): void {
    this.init();
    this.tone(659.25, 0.25, 0.14, 'sine');
    setTimeout(() => this.tone(880, 0.4, 0.14, 'sine'), 100);
  }

  denied(): void {
    this.init();
    this.tone(180, 0.25, 0.18, 'sawtooth');
    setTimeout(() => this.tone(140, 0.3, 0.14, 'sawtooth'), 80);
  }

  private tone(freq: number, dur = 0.8, vol = 0.3, type: OscillatorType = 'sine', detune = 0): void {
    if (!this.ctx || this.muted || !this.masterGain) return;
    const ctx = this.ctx;
    const t0 = ctx.currentTime;
    const osc = ctx.createOscillator();
    const g = ctx.createGain();
    osc.type = type;
    osc.frequency.value = freq;
    osc.detune.value = detune;
    g.gain.setValueAtTime(0, t0);
    g.gain.linearRampToValueAtTime(vol, t0 + 0.01);
    g.gain.exponentialRampToValueAtTime(0.0001, t0 + dur);
    osc.connect(g).connect(this.masterGain);
    osc.start(t0);
    osc.stop(t0 + dur + 0.05);
  }

  private chord(freqs: number[], dur: number, vol: number, type: OscillatorType = 'sine'): void {
    freqs.forEach((f, i) => this.tone(f, dur, vol, type, (i - 1) * 4));
  }
}
