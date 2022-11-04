using System.Diagnostics;
using Fody;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace MongoIncUpdate.Fody;

public partial class ModuleWeaver : BaseModuleWeaver
{
    private readonly TypeSelector _typeSelector;

    public ModuleWeaver()
        : this(new TypeSelector())
    {
        Debugger.Launch(); 
    }

    public ModuleWeaver( 
        TypeSelector typeSelector)
    {
        _typeSelector = typeSelector;
    }

    public override void Execute()
    {
        //查找类型引用
        FindCoreReferences();

        //获取带有MongoIncUpdateAttribute属性的类(也支持继承至某类,后续补充实现)
        var selectedTypes = _typeSelector.Select(ModuleDefinition);
        

        _typeSelector.MustAllContainerAndProperty(selectedTypes);

        foreach (var typ in selectedTypes)
        {
            //继承这个接口
            typ.Interfaces.Add(new InterfaceImplementation(_mongoIncUpdateInterface));

            //插入init函数调用
            //init方法
            var tempInitMethodDef = _typeSelector.SelectMethodFromType(_mongoIncUpdateInterface, "Init");
            var initMethodDef = ModuleDefinition.ImportReference(tempInitMethodDef);
            var ctors = typ.Methods.Where(m => m.IsConstructor).ToList();
            foreach (var ctor in ctors)
            {
                var last = ctor.Body.Instructions.Count - 1; //插入到ret之前的位置
                ctor.Body.Instructions.Insert(last,
                    Instruction.Create(OpCodes.Ldarg_0),
                    Instruction.Create(OpCodes.Callvirt, initMethodDef),
                    Instruction.Create(OpCodes.Nop));
            }

            //脏标记
            InjectOverrideProperty(typ, "Dirties");
            //IdxMapping
            InjectOverrideProperty(typ, "IdxMapping");
            //NameMapping
            InjectOverrideProperty(typ, "NameMapping");

            //注入属性变化监听
            InjectPropSetterPropChangeNotify(typ);
        }

        //获取所有符合条件的类
        //先处理继承 1继承,2属性实现
        //处理属性 1.获取需要监听setter的属性,2.调用继承的方法Dirties[idx]=true;
    }

    public override IEnumerable<string> GetAssembliesForScanning()
    {
        yield return "mscorlib";
        yield return "System";
        yield return "System.Collections";
    }
}