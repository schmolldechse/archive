---
name: designing-editorial-interfaces
description: Use when designing or reviewing editorial, archival, historical, or provenance-heavy interfaces that need a calm visual system for color, typography, metadata, timelines, snapshots, or dense information.
---

# Designing Editorial Interfaces

## Overview

Treat the interface as a digital reading room: quiet, trustworthy, and structured around source, time, and provenance. Favor editorial hierarchy and precise records over dashboard spectacle.

Before creating or changing a relevant interface, read [references/design-system.md](references/design-system.md). It is the source of truth for palette names and values, typography, accessibility, spacing, component treatment, and interaction states.

## Apply the System

1. Establish the English design tokens before styling components. Keep light and dark themes semantically equivalent without treating dark mode as an inversion.
2. Assign typography by meaning: Newsreader for editorial significance, Source Sans 3 for interface and reading, and IBM Plex Mono for machine-readable records.
3. Build hierarchy with horizontal rules, whitespace, rhythm, dates, and provenance. Prefer structured lists and definition lists to grids of equal cards.
4. Encode interaction states with at least two signals. Pair color with text, icon, shape, position, weight, or a semantic attribute.
5. Check contrast, focus visibility, target size, reflow, URL wrapping, reduced motion, and the separation between archived content and archive controls.

## Quick Reference

| Role | Light | Dark |
|---|---|---|
| Reading room | Morning Paper `#F7F4EC` | Night Stacks `#0B1113` |
| Secondary layer | Archive Dust `#E5DFD2` | Charcoal Fold `#172125` |
| Reading voice | Chronicle Ink `#18201F` | Light Note `#ECE9DF` |
| Interaction | Register Blue `#2E52C7` | Register Blue `#8EA7FF` |
| Selected time | Time Copper `#A43C2E` | Time Copper `#FF9A7A` |

## Non-Negotiable Character

- Reading, source, and time lead; decoration remains subordinate.
- Time appears through register lines, date notation, sequences, bands, and layered records—not clocks or nostalgic props.
- Surfaces remain quiet: few color planes, fine outlines, restrained shadows, and mostly rectangular geometry.
- Avoid decorative gradients, glassmorphism, neon, paper textures, sepia filters, oversized radii, floating cards, KPI-card homepages, and perpetual animation.
- Never recolor an archived webpage to match the surrounding dark theme.

## Common Mistakes

- Using Register Blue and Time Copper as competing actions instead of giving each a distinct semantic role.
- Truncating a URL without exposing the complete copyable value.
- Using color alone for selection, errors, success, or chart meaning.
- Applying monospace broadly instead of reserving it for URLs, timestamps, IDs, checksums, sizes, and integrity data.
- Adding containers or shadows where a rule and spacing would create clearer hierarchy.
