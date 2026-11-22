namespace Proctal.Core;
public enum ShortcutAction { None, Toggle, Answer, Pause, Interactive, Up, Down, Left, Right, Copy, Screenshot,
    ScrollUp,ScrollDown,PageUp,PageDown,ScrollTop,ScrollEnd,ResizeLeft,ResizeRight,ResizeUp,ResizeDown,
    OpacityDown,OpacityUp,ZoomOut,ZoomIn,CopyCode,Compact,WrapCode,Follow,PreviousAnswer,NextAnswer,LatestAnswer,
    Explain,EdgeCases,Optimize,ResetLayout,EditQuestion,ExportAnswer,Glass,
    Captions,Tune,AutoAnswer,AlwaysOnTop,CaptionPageUp,CaptionPageDown,CaptionTop,CaptionEnd,Pin,PracticeQuestion,ReviewAnswer,ClearImages,CancelAnswer,AnswerImages,DeepAnswer,History,SolAnswer }
public enum AudioSource { Interviewer, Candidate }
public record TranscriptTurn(string ItemId, AudioSource Source, string Text, bool IsFinal, DateTimeOffset At)
{ public DateTimeOffset? CompletedAt {get;init;} }
public record ContextDocument(string Id, string Name, string Text);
public record ContextPassage(string Source, string Text);
public record AnswerRecord(string Id, string Question, string Answer, DateTimeOffset At);
public record ImageAttachment(string Name,string DataUrl);
