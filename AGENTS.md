# AI Agent Guidelines & Unity Integration for Slime-Nindja

## Unity CLI & MCP Integration

This project is configured with the official **Unity CLI** (`unity`) and the **Unity MCP Server** via the **Unity Pipeline** package (`com.unity.pipeline`).

### Quick Status Check
- Unity CLI binary: `/home/moonsprocket/.local/bin/unity`
- Running Unity Editor: Unity 6.6 (`6000.6.5f1`)
- Local Pipeline API: `http://127.0.0.1:7800/api/editor_status`

To verify Editor connection:
```bash
unity status
unity command editor_status
```

### Model Context Protocol (MCP) Configuration
MCP allows AI coding assistants to directly inspect, query, and manipulate the Unity project and Editor in real time:

- **Antigravity**: Configured in `~/.gemini/config/mcp_config.json`
- **VS Code / Copilot**: Configured in `.vscode/mcp.json`
- **MCP Server Stdio Command**: `unity mcp`
- **Skill Documentation**: `.agents/skills/unity-cli/SKILL.md` (and globally in `~/.gemini/config/skills/unity-cli/`)

### Key Unity CLI Commands
- `unity status` — Check status of connected Unity Editors.
- `unity command <name>` — Run commands on the running Editor instance.
- `unity command eval '<C# code>'` — Execute arbitrary C# code on the main thread in milliseconds.
- `unity command editor_play` — Start Play mode.
- `unity command editor_pause` — Pause/resume Play mode.
- `unity command editor_stop` — Stop Play mode.
- `unity command get_scene_hierarchy` — Dump the hierarchy of the currently open scene.
- `unity test` — Run automated EditMode/PlayMode tests.
- `unity recompile` — Trigger script compilation and check for errors.
- `unity mcp` — Run the standalone MCP stdio server.
