using Fody;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace MongoIncUpdate.Fody;

public partial class ModuleWeaver : BaseModuleWeaver
{
    private readonly CallMapper _callMapper;

    private readonly MemberSelector _memberSelector;
    private readonly MemberVirtualizer _memberVirtualizer;
    private readonly TypeSelector _typeSelector;

    public ModuleWeaver()
        : this(new TypeSelector(), new MemberSelector(), new MemberVirtualizer(), new CallMapper())
    {
        // Debugger.Launch();
    }

    public ModuleWeaver(
        TypeSelector typeSelector,
        MemberSelector memberSelector,
        MemberVirtualizer memberVirtualizer,
        CallMapper callMapper)
    {
        _typeSelector = typeSelector;
        _memberSelector = memberSelector;
        _memberVirtualizer = memberVirtualizer;
        _callMapper = callMapper;
    }

    public override void Execute()
    {
        //查找类型引用
        FindCoreReferences();

        //获取带有MongoIncUpdateAttribute属性的类(也支持继承至某类,后续补充实现)
        var selectedTypes = _typeSelector.Select(ModuleDefinition);

        //合法性检查
        _typeSelector.CheckTypeLegal(selectedTypes);

        foreach (var typ in selectedTypes)
        {
            //继承这个接口
            typ.Interfaces.Add(new InterfaceImplementation(MongoIncUpdateInterface));

            //插入init函数调用
            //init方法
            var initMethodDef = _typeSelector.SelectMethodFromType(ModuleDefinition, MongoIncUpdateInterface, "Init");
            var ctors = typ.Methods.Where(m => m.IsConstructor).ToList();
            foreach (var ctor in ctors)
            {
                var last = ctor.Body.Instructions.Count - 1; //插入到ret之前的位置
                ctor.Body.Instructions.Insert(last,
                    Instruction.Create(OpCodes.Ldarg_0),
                    Instruction.Create(OpCodes.Callvirt, initMethodDef),
                    Instruction.Create(OpCodes.Nop));
            }

            //一定要先做
            //注入属性变化监听（警告:一定不要调换顺序,一定要先注入监听,因为这里有属性遍历,避免属性被后面污染了）
            InjectPropSetterPropChangeNotify(typ);
            //
            //再后做------因为注入属性会影响前面的注入监听
            //脏标记
            InjectOverrideProperty(typ, "Dirties");
            //Init
            InjectOverrideProperty(typ, "IsOnceInitDone", true);
            //NameMapping
            InjectOverrideProperty(typ, "NameMapping", true);
            //IdxMapping
            InjectOverrideProperty(typ, "IdxMapping", true);
        }
    }

    public override IEnumerable<string> GetAssembliesForScanning()
    {
        yield return "mscorlib";
        yield return "System";
    }
}