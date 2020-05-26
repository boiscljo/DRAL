namespace AttentionAndRetag.Retag
{
    public interface IClonable<T>:IObjectClonable
	{
		T Clone();
	}
}
