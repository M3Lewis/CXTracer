namespace CXTracer.Models;

public enum EventKind
{
    User,
    Assistant,
    Plan,
    Final,
    Reasoning,
    Command,
    CommandOutput,
    Diff,
    Tool,
    ToolCall,
    ToolResult,
    Error,
    Raw
}
