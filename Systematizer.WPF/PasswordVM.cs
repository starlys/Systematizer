﻿namespace Systematizer.WPF;

class PasswordVM : EditableVM
{
    string _value; 
    public string Value
    {
        get => _value;
        set { _value = value; NotifyChanged(); }
    }

    bool _isRevealedExplicitly;
    public bool IsRevealed
    {
        get => _isRevealedExplicitly || string.IsNullOrEmpty(Value);
        set
        {
            _isRevealedExplicitly = value;
            NotifyChanged(); 
            NotifyChanged(nameof(ValueVisibility));
            NotifyChanged(nameof(RevealButtonVisibility));
        }
    }

    public Visibility ValueVisibility => ToVisibility(IsRevealed);

    public Visibility RevealButtonVisibility => ToVisibility(!IsRevealed);
}
