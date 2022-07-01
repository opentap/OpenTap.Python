namespace OpenTap.Python;

public class VirtualValidationRule : ValidationRule
{
    public virtual string Error()
    {
        return null;
    }
    public override string ErrorMessage 
    { 
        get => Error();
        set { } 
    }

    public VirtualValidationRule(string property) : base(null, null, property)
    {
        this.IsValid = () => Error() == null;
    }
}