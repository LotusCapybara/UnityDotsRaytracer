namespace CapyTracerCore.Core
{
    public enum EStepEvent
    {
        BHVCalculated,
        DirectSample,
        IndirectSampleIteration,
        FinishedAll
    }

    public enum ERenderTextureType
    {
        FinalRender,
        DirectBuffer,
        IndirectBuffer,
        BHVDebug
    }
}