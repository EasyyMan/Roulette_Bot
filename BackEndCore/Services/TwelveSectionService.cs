
namespace BackEndCore.Services;

internal class TwelveSectionService
{
    private int _currentSectionIndex = 2;
    private int _lossCount = 0;

    public int GetNextSectionIndex()
    {
        if (_currentSectionIndex == 1)
        {
            _currentSectionIndex = 2;
        }
        else
        {
            _currentSectionIndex = 1;
        }

        return _currentSectionIndex;
    }


    public void AddLoss()
    {
        _lossCount++;
    }

    public void ResetLossCount()
    {
        _lossCount = 0;
    }


    public double GetNextChipAmount(double chipAmount, ChipAmountCalc_12section strategy)
    {
        return strategy switch
        {
            ChipAmountCalc_12section.SimpleDouble => chipAmount * 2,// 1, 2, 4, 8, 16...

            ChipAmountCalc_12section.DoublePlusOne => chipAmount * 2 + 1,// 1, 3, 7, 15, 31, 63...

            ChipAmountCalc_12section.DoublePlusIncrementingDollar => chipAmount * 2 + _lossCount,// 1, 3, 8, 19, 42, 89...

            ChipAmountCalc_12section.DoubleThenTriple => _lossCount == 1 ? chipAmount * 2 : chipAmount * 3,// 1, 2, 6, 18, 54...

            _ => throw new ArgumentOutOfRangeException(nameof(strategy)),
        };
    }

}
