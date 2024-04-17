using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using MD_SW_ConnectSW;
using PropertyEditingTool.Models;
using SolidWorks.Interop.sldworks;

namespace PropertyEditingTool;

internal class SldWorkService
{
    public SldWorks swApp;

    private ModelDoc2 swDoc;

    public bool ActiveDocIsAsm()
    {
        swDoc = (ModelDoc2)(dynamic)swApp.ActiveDoc;
        if (swDoc == null)
        {
            return false;
        }
        return Path.GetExtension(swDoc.GetPathName()).ToUpper() == ".SLDASM";
    }

    public void OpenDoc(SwFile swFile)
    {
        swDoc = (dynamic)swApp.ActivateDoc(swFile.FullName);
        if (swDoc == null)
        {
            SldWorks sldWorks = swApp;
            string fullName = swFile.FullName;
            int fileType_Int = swFile.FileType_Int;
            int Errors = 0;
            int Warnings = 0;
            swDoc = sldWorks.OpenDoc6(fullName, fileType_Int, 1, "", ref Errors, ref Warnings);
        }
        if (swDoc == null)
        {
            throw new Exception("“" + swFile.FullName + "”模型版本过高，当前连接sw无法打开");
        }
    }

    public string GetActiveFileName()
    {
        try
        {
            swDoc = (ModelDoc2)(dynamic)swApp.ActiveDoc;
            return swDoc.GetPathName();
        }
        catch (Exception)
        {
            throw new Exception("当前活动窗口未打开文件");
        }
    }

    public string[] GetActiveAllFileName()
    {
        List<string> listFile = new List<string>();
        if (swApp.GetDocumentCount() == 0)
        {
            throw new Exception("当前活动窗口未打开文件");
        }
        foreach (object item in (dynamic)swApp.GetDocuments())
        {
            ModelDoc2 doc = (ModelDoc2)(dynamic)item;
            if (doc.Visible)
            {
                listFile.Add(doc.GetPathName());
            }
        }
        return listFile.ToArray();
    }

    public string[] GetSelectedItem()
    {
        List<string> listFile = new List<string>();
        swDoc = (ModelDoc2)(dynamic)swApp.ActiveDoc;
        if (swDoc == null)
        {
            throw new Exception("您未打开任何模型");
        }
        swDoc.GetPathName();
        if (swDoc.GetType() == 2)
        {
            SelectionMgr mgr = (dynamic)swDoc.SelectionManager;
            for (int i = 1; i < mgr.GetSelectedObjectCount() + 1; i++)
            {
                Component2 com = null;
                dynamic objSelected = mgr.GetSelectedObject6(i, -1);
                if (objSelected is Component2)
                {
                    com = objSelected as Component2;
                }
                else
                {
                    dynamic val = objSelected.GetType() == 2;
                    if (!(val ? true : false) && !((val | (objSelected.GetType() == 1)) ? true : false))
                    {
                        continue;
                    }
                    com = (objSelected as Entity).IGetComponent2();
                }
                listFile.Add(com.GetPathName());
            }
            return listFile.ToArray();
        }
        throw new Exception("您未打开装配体模型");
    }

    public void CloseDoc(bool isSave)
    {
        swDoc = (ModelDoc2)(dynamic)swApp.ActiveDoc;
        string model = swDoc.GetTitle();
        if (isSave)
        {
            swDoc.Save();
        }
        swApp.CloseDoc(model);
    }

    private string AnalysisCusPropValue(string value)
    {
        swDoc = (ModelDoc2)(dynamic)swApp.ActiveDoc;
        if (value.Contains("[单重]"))
        {
            value = value.Replace("[单重]", "");
            return "\"" + value + "@" + swDoc.GetTitle() + "\"";
        }
        if (value.Contains("[材料]"))
        {
            value = value.Replace("[材料]", "");
            return "\"" + value + "@" + swDoc.GetTitle() + "\"";
        }
        if (value.Contains("[密度]"))
        {
            value = value.Replace("[密度]", "");
            return "\"" + value + "@" + swDoc.GetTitle() + "\"";
        }
        if (value.Contains("[体积]"))
        {
            value = value.Replace("[体积]", "");
            return "\"" + value + "@" + swDoc.GetTitle() + "\"";
        }
        if (value.Contains("[表面积]"))
        {
            value = value.Replace("[表面积]", "");
            return "\"" + value + "@" + swDoc.GetTitle() + "\"";
        }
        return value;
    }

    private string AnalysisConfigPropValue(string value, string configName)
    {
        swDoc = (ModelDoc2)(dynamic)swApp.ActiveDoc;
        if (value.Contains("[单重]"))
        {
            value = value.Replace("[单重]", "");
            return "\"" + value + "@@" + configName + "@" + swDoc.GetTitle() + "\"";
        }
        if (value.Contains("[材料]"))
        {
            value = value.Replace("[材料]", "");
            return "\"" + value + "@@" + configName + "@" + swDoc.GetTitle() + "\"";
        }
        if (value.Contains("[密度]"))
        {
            value = value.Replace("[密度]", "");
            return "\"" + value + "@@" + configName + "@" + swDoc.GetTitle() + "\"";
        }
        if (value.Contains("[体积]"))
        {
            value = value.Replace("[体积]", "");
            return "\"" + value + "@@" + configName + "@" + swDoc.GetTitle() + "\"";
        }
        if (value.Contains("[表面积]"))
        {
            value = value.Replace("[表面积]", "");
            return "\"" + value + "@@" + configName + "@" + swDoc.GetTitle() + "\"";
        }
        return value;
    }

    public void EditConfigProp(SwProperty prop)
    {
        CustomPropertyManager swCusPropMgr = ((Configuration)(dynamic)swDoc.GetConfigurationByName(prop.ConfigName)).CustomPropertyManager;
        if (prop.New_Value != null)
        {
            prop.New_Value = AnalysisConfigPropValue(prop.New_Value.ToString(), prop.ConfigName);
        }
        if (prop.Old_Value != null)
        {
            prop.Old_Value = AnalysisConfigPropValue(prop.Old_Value, prop.ConfigName);
        }
        string[] names = (dynamic)swCusPropMgr.GetNames();
        if (names == null)
        {
            names = new string[0];
        }
        switch (prop.EditType)
        {
            case "修改属性名":
                {
                    if (names.Contains(prop.New_Name))
                    {
                        prop.ErrorInfo = "属性名“" + prop.New_Name + "”已存在";
                        break;
                    }
                    if (!names.Contains(prop.Old_Name))
                    {
                        prop.ErrorInfo = "原属性名“" + prop.Old_Name + "”不存在";
                        break;
                    }
                    string value = swCusPropMgr.Get(prop.Old_Name);
                    prop.New_Value = value;
                    swCusPropMgr.Add2(prop.New_Name, prop.Type, (prop.New_Value == null) ? "" : prop.New_Value.ToString());
                    swCusPropMgr.Delete(prop.Old_Name);
                    prop.ErrorInfo = "修改成功";
                    break;
                }
            case "原名 → 新值":
                if (!names.Contains(prop.Old_Name))
                {
                    prop.ErrorInfo = "原属性名“" + prop.Old_Name + "”不存在";
                    break;
                }
                swCusPropMgr.Set(prop.Old_Name, prop.New_Value.ToString());
                prop.ErrorInfo = "修改成功";
                break;
            case "原值 → 新值":
                {
                    dynamic propNames2 = null;
                    object propTypes2 = null;
                    dynamic propValues2 = null;
                    swCusPropMgr.GetAll(ref propNames2, ref propTypes2, ref propValues2);
                    if (propValues2 == null)
                    {
                        break;
                    }
                    if (((string[])propValues2).Count((string i) => i == prop.Old_Value) == 0)
                    {
                        prop.ErrorInfo = "属性值“" + prop.Old_Value + "”不存在";
                        break;
                    }
                    for (int k = 0; k < propValues2.Length; k++)
                    {
                        if (propValues2[k] == prop.Old_Value)
                        {
                            swCusPropMgr.Set(propNames2[k], prop.New_Value.ToString());
                        }
                    }
                    prop.ErrorInfo = "修改成功";
                    break;
                }
            case "（原名、原值） → 新值":
                if (!names.Contains(prop.Old_Name))
                {
                    prop.ErrorInfo = "原属性名“" + prop.Old_Name + "”不存在";
                }
                else if (swCusPropMgr.Get(prop.Old_Name) != prop.Old_Value)
                {
                    prop.ErrorInfo = "不存在属性名为：“" + prop.Old_Name + "”、属性值为：“" + prop.Old_Value + "”的属性";
                }
                else
                {
                    swCusPropMgr.Set(prop.Old_Name, prop.New_Value.ToString());
                    prop.ErrorInfo = "修改成功";
                }
                break;
            case "添加属性":
                if (names.Contains(prop.New_Name))
                {
                    prop.ErrorInfo = "属性名“" + prop.New_Name + "”已存在";
                    break;
                }
                swCusPropMgr.Add2(prop.New_Name, prop.Type, prop.New_Value.ToString());
                prop.ErrorInfo = "修改成功";
                break;
            case "关键字替换":
                {
                    dynamic propNames = null;
                    object propTypes = null;
                    dynamic propValues = null;
                    string oldKey2 = prop.Old_Value.TrimStart('{').TrimEnd('}');
                    swCusPropMgr.GetAll(ref propNames, ref propTypes, ref propValues);
                    if (propValues == null)
                    {
                        break;
                    }
                    if (((string[])propValues).Count((string i) => i.Contains(oldKey2)) == 0)
                    {
                        prop.ErrorInfo = "关键字“" + oldKey2 + "”不存在";
                        break;
                    }
                    for (int j = 0; j < propValues.Length; j++)
                    {
                        if (propValues[j].Contains(oldKey2))
                        {
                            string newValue2 = propValues[j].Replace(oldKey2, prop.New_Value.ToString());
                            swCusPropMgr.Set(propNames[j], newValue2);
                        }
                    }
                    prop.ErrorInfo = "修改成功";
                    break;
                }
            case "关键字替换2":
                {
                    if (!names.Contains(prop.Old_Name))
                    {
                        prop.ErrorInfo = "原属性名“" + prop.Old_Name + "”不存在";
                        break;
                    }
                    string oldKey = prop.Old_Value.TrimStart('{').TrimEnd('}');
                    string oldValue = swCusPropMgr.Get(prop.Old_Name);
                    if (oldValue.Contains(oldKey))
                    {
                        string newValue = oldValue.Replace(oldKey, prop.New_Value.ToString());
                        swCusPropMgr.Set(prop.Old_Name, newValue);
                        prop.ErrorInfo = "修改成功";
                        break;
                    }
                    prop.ErrorInfo = "属性“" + prop.Old_Name + "”不存在关键字“" + oldKey + "”";
                    break;
                }
            default:
                prop.ErrorInfo = "修改规则无法解析。";
                break;
        }
    }

    public void EditCutProp(SwFile swFile, List<SwProperty> swProps)
    {
        swFile.listSwProperty = FileHelper.Clone(swProps);
        swDoc = (ModelDoc2)(dynamic)swApp.ActiveDoc;
        Configuration config = (dynamic)swDoc.GetActiveConfiguration();
        foreach (SwProperty prop in swFile.listSwProperty)
        {
            prop.FullName = swFile.FullName;
            prop.Name = swFile.Name;
            prop.ConfigName = config.Name;
            EditConfigProp(prop);
        }
    }

    public void EditAllProp(SwFile swFile, List<SwProperty> swProps)
    {
        swDoc = (ModelDoc2)(dynamic)swApp.ActiveDoc;
        string[] obj = (dynamic)swDoc.GetConfigurationNames();
        List<SwProperty> listSwProp = new List<SwProperty>();
        string[] array = obj;
        foreach (string configName in array)
        {
            List<SwProperty> temp = FileHelper.Clone(swProps);
            temp.ForEach(delegate (SwProperty prop)
            {
                prop.Name = swFile.Name;
                prop.FullName = swFile.FullName;
                prop.ConfigName = configName;
            });
            listSwProp.AddRange(temp);
        }
        swFile.listSwProperty = listSwProp;
        foreach (SwProperty prop2 in swFile.listSwProperty)
        {
            EditConfigProp(prop2);
        }
    }

    public void EditCustomInfo(SwProperty prop) // 属性编辑
    {
        swDoc = (ModelDoc2)(dynamic)swApp.ActiveDoc;
        if (prop.New_Value != null)
        {
            prop.New_Value = AnalysisCusPropValue(prop.New_Value.ToString());
        }
        if (prop.Old_Value != null)
        {
            prop.Old_Value = AnalysisCusPropValue(prop.Old_Value);
        }
        string[] propNames = (dynamic)swDoc.GetCustomInfoNames();
        if (propNames == null)
        {
            propNames = new string[0];
        }
        switch (prop.EditType)
        {
            case "修改属性名":
                {
                    if (propNames.Contains(prop.New_Name))
                    {
                        prop.ErrorInfo = "属性名“" + prop.New_Name + "”已存在";
                        break;
                    }
                    if (!propNames.Contains(prop.Old_Name))
                    {
                        prop.ErrorInfo = "原属性名“" + prop.Old_Name + "”不存在";
                        break;
                    }
                    string value = ((IModelDoc2)swDoc).get_CustomInfo(prop.Old_Name);
                    prop.New_Value = value;
                    swDoc.AddCustomInfo2(prop.New_Name, prop.Type, (prop.New_Value == null) ? string.Empty : prop.New_Value.ToString());
                    swDoc.DeleteCustomInfo(prop.Old_Name);
                    prop.ErrorInfo = "修改成功";
                    break;
                }
            case "原名 → 新值":
                if (!propNames.Contains(prop.Old_Name))
                {
                    prop.ErrorInfo = "原属性名“" + prop.Old_Name + "”不存在";
                    break;
                }
            ((IModelDoc2)swDoc).set_CustomInfo(prop.Old_Name, prop.New_Value.ToString());
                prop.ErrorInfo = "修改成功";
                break;
            case "原值 → 新值":
                {
                    bool hasOldValue2 = false;
                    string[] array = propNames;
                    foreach (string name2 in array)
                    {
                        if (((IModelDoc2)swDoc).get_CustomInfo(name2) == prop.Old_Value)
                        {
                            ((IModelDoc2)swDoc).set_CustomInfo(name2, prop.New_Value.ToString());
                            hasOldValue2 = true;
                        }
                    }
                    if (!hasOldValue2)
                    {
                        prop.ErrorInfo = "属性值“" + prop.Old_Value + "”不存在";
                    }
                    else
                    {
                        prop.ErrorInfo = "修改成功";
                    }
                    break;
                }
            case "（原名、原值） → 新值":
                if (!propNames.Contains(prop.Old_Name))
                {
                    prop.ErrorInfo = "原属性名“" + prop.Old_Name + "”不存在";
                }
                else if (((IModelDoc2)swDoc).get_CustomInfo(prop.Old_Name) != prop.Old_Value)
                {
                    prop.ErrorInfo = "不存在属性名为：“" + prop.Old_Name + "”、属性值为：“" + prop.Old_Value + "”的属性";
                }
                else
                {
                    ((IModelDoc2)swDoc).set_CustomInfo(prop.Old_Name, prop.New_Value.ToString());
                    prop.ErrorInfo = "修改成功";
                }
                break;
            case "添加属性":
                if (propNames.Contains(prop.New_Name))
                {
                    prop.ErrorInfo = "属性名“" + prop.New_Name + "”已存在";
                    break;
                }
                swDoc.AddCustomInfo2(prop.New_Name, prop.Type, prop.New_Value.ToString());
                prop.ErrorInfo = "修改成功";
                break;
            case "关键字替换":
                {
                    bool hasOldValue = false;
                    string oldKey2 = prop.Old_Value.TrimStart('{').TrimEnd('}');
                    string[] array = propNames;
                    foreach (string name in array)
                    {
                        string propValue = ((IModelDoc2)swDoc).get_CustomInfo(name);
                        if (propValue.Contains(oldKey2))
                        {
                            string newValue2 = propValue.Replace(oldKey2, prop.New_Value.ToString());
                            ((IModelDoc2)swDoc).set_CustomInfo(name, newValue2);
                            hasOldValue = true;
                        }
                    }
                    if (!hasOldValue)
                    {
                        prop.ErrorInfo = "关键字“" + oldKey2 + "”不存在";
                    }
                    else
                    {
                        prop.ErrorInfo = "修改成功";
                    }
                    break;
                }
            case "关键字替换2":
                {
                    if (!propNames.Contains(prop.Old_Name))
                    {
                        prop.ErrorInfo = "原属性名“" + prop.Old_Name + "”不存在";
                        break;
                    }
                    string oldKey = prop.Old_Value.TrimStart('{').TrimEnd('}');
                    string oldValue = ((IModelDoc2)swDoc).get_CustomInfo(prop.Old_Name);
                    if (oldValue.Contains(oldKey))
                    {
                        string newValue = oldValue.Replace(oldKey, prop.New_Value.ToString());
                        ((IModelDoc2)swDoc).set_CustomInfo(prop.Old_Name, newValue);
                        prop.ErrorInfo = "修改成功";
                        break;
                    }
                    prop.ErrorInfo = "属性“" + prop.Old_Name + "”不存在关键字“" + oldKey + "”";
                    break;
                }
            case "拷贝(新名 → 旧值)":
                {
                    break;
                }
            default:
                prop.ErrorInfo = "修改规则无法解析。";
                break;
        }
    }

    public void EditCustomInfoByFile(SwFile swFile, List<SwProperty> swProps)
    {
        List<SwProperty> cusList = FileHelper.Clone(swProps);
        cusList.ForEach(delegate (SwProperty prop)
        {
            prop.Name = swFile.Name;
            prop.FullName = swFile.FullName;
            prop.ConfigName = "自定义属性";
            EditCustomInfo(prop);
        });
        swFile.listSwProperty.AddRange(cusList);
    }

    private string[] GetCusPropNameList(int type, List<string> listPropName)
    {
        swDoc = (ModelDoc2)(dynamic)swApp.ActiveDoc;
        string[] propNames = null;
        string[] allPropName = (dynamic)swDoc.GetCustomInfoNames();
        switch (type)
        {
            case 1:
                propNames = allPropName;
                break;
            case 2:
                propNames = listPropName.Where((string name) => allPropName.Contains(name)).ToArray();
                break;
            case 3:
                {
                    List<string> names = allPropName.ToList();
                    listPropName.ForEach(delegate (string i)
                    {
                        names.Remove(i);
                    });
                    propNames = names.ToArray();
                    break;
                }
        }
        return propNames;
    }

    private string[] GetConfigPropNameList(int type, List<string> listPropName)
    {
        swDoc = (ModelDoc2)(dynamic)swApp.ActiveDoc;
        CustomPropertyManager swCusPropMgr = ((Configuration)(dynamic)swDoc.GetActiveConfiguration()).CustomPropertyManager;
        string[] propNames = null;
        string[] allPropName = (dynamic)swCusPropMgr.GetNames();
        if (allPropName == null)
        {
            return propNames;
        }
        switch (type)
        {
            case 1:
                propNames = allPropName;
                break;
            case 2:
                propNames = listPropName.Where((string name) => allPropName.Contains(name)).ToArray();
                break;
            case 3:
                {
                    List<string> names = allPropName.ToList();
                    listPropName.ForEach(delegate (string i)
                    {
                        names.Remove(i);
                    });
                    propNames = names.ToArray();
                    break;
                }
        }
        return propNames;
    }

    public void ReplaceProp(SwProperty prop)
    {
        if (prop.ConfigName != "自定义属性")
        {
            ((Configuration)(dynamic)swDoc.GetConfigurationByName(prop.ConfigName)).CustomPropertyManager.Set(prop.New_Name, prop.New_Value.ToString());
        }
        else
        {
            ((IModelDoc2)swDoc).set_CustomInfo(prop.New_Name, prop.New_Value.ToString());
        }
        prop.ErrorInfo = "修改成功";
    }

    public void CustomTransferToConfig(SwFile swFile, int type, List<string> listPropName)
    {
        string[] propNames = GetCusPropNameList(type, listPropName);
        if (propNames == null || propNames.Length == 0)
        {
            return;
        }
        swFile.listSwProperty = new List<SwProperty>();
        string configName = ((dynamic)swDoc.GetActiveConfiguration()).Name;
        string[] array = propNames;
        foreach (string propName in array)
        {
            swFile.listSwProperty.Add(new SwProperty
            {
                FullName = swFile.FullName,
                Name = swFile.Name,
                ConfigName = configName,
                New_Name = propName,
                New_Value = ((IModelDoc2)swDoc).get_CustomInfo(propName)
            });
        }
        foreach (SwProperty swProp in swFile.listSwProperty)
        {
            EditConfigProp(swProp);
        }
    }

    public void ConfigTransferToCustom(SwFile swFile, int type, List<string> listPropName)
    {
        string[] propNames = GetConfigPropNameList(type, listPropName);
        if (propNames == null || propNames.Length == 0)
        {
            return;
        }
        swFile.listSwProperty = new List<SwProperty>();
        CustomPropertyManager swCusPropMgr = ((Configuration)(dynamic)swDoc.GetActiveConfiguration()).CustomPropertyManager;
        string[] array = propNames;
        foreach (string propName in array)
        {
            swFile.listSwProperty.Add(new SwProperty
            {
                FullName = swFile.FullName,
                Name = swFile.Name,
                ConfigName = "自定义属性",
                New_Name = propName,
                New_Value = swCusPropMgr.Get(propName)
            });
        }
        foreach (SwProperty prop in swFile.listSwProperty)
        {
            EditCustomInfo(prop);
        }
    }

    /// <summary>
    /// 删除自定义属性
    /// </summary>
    /// <param name="swFile"></param>
    /// <param name="type"></param>
    /// <param name="listPropName"></param>
    public void DelectCustomInfo(SwFile swFile, int type, List<string> listPropName)
    {
        string[] propNames = GetCusPropNameList(type, listPropName);
        if (propNames == null || propNames.Length == 0)
        {
            return;
        }
        swFile.listSwProperty = propNames.Select((string propName) => new SwProperty
        {
            FullName = swFile.FullName,
            Name = swFile.Name,
            ConfigName = "自定义属性",
            Old_Name = propName
        }).ToList();
        swDoc = (ModelDoc2)(dynamic)swApp.ActiveDoc;
        foreach (SwProperty prop in swFile.listSwProperty)
        {
            if (swDoc.DeleteCustomInfo(prop.Old_Name))
            {
                prop.ErrorInfo = "修改成功";
            }
            else
            {
                prop.ErrorInfo = "属性“" + prop.Old_Name + "”不存在";
            }
        }
    }

    /// <summary>
    /// 删除配置义属性
    /// </summary>
    /// <param name="swFile"></param>
    /// <param name="type"></param>
    /// <param name="listPropName"></param>
    public void DelectConfigProp(SwFile swFile, int type, List<string> listPropName)
    {
        string[] propNames = GetConfigPropNameList(type, listPropName);
        if (propNames == null || propNames.Length == 0)
        {
            return;
        }
        Configuration config = (dynamic)swDoc.GetActiveConfiguration();
        CustomPropertyManager swCusPropMgr = config.CustomPropertyManager;
        swFile.listSwProperty = propNames.Select((string propName) => new SwProperty
        {
            FullName = swFile.FullName,
            Name = swFile.Name,
            ConfigName = config.Name,
            Old_Name = propName
        }).ToList();
        foreach (SwProperty prop in swFile.listSwProperty)
        {
            if (swCusPropMgr.Delete(prop.Old_Name) == 0)
            {
                prop.ErrorInfo = "修改成功";
            }
            else
            {
                prop.ErrorInfo = "属性“" + prop.Old_Name + "”不存在";
            }
        }
    }

    public void DelectAllConfigProp(SwFile swFile, int type, List<string> listPropName)
    {
        swDoc = (ModelDoc2)(dynamic)swApp.ActiveDoc;
        string[] obj = (dynamic)swDoc.GetConfigurationNames();
        swFile.listSwProperty = new List<SwProperty>();
        string[] array = obj;
        foreach (string configName in array)
        {
            string[] propNames = GetConfigPropNameList(type, listPropName);
            if (propNames != null && propNames.Length != 0)
            {
                IEnumerable<SwProperty> list = propNames.Select((string propName) => new SwProperty
                {
                    FullName = swFile.FullName,
                    Name = swFile.Name,
                    ConfigName = configName,
                    Old_Name = propName
                });
                swFile.listSwProperty.AddRange(list);
            }
        }
        foreach (SwProperty prop in swFile.listSwProperty)
        {
            if (((Configuration)(dynamic)swDoc.GetConfigurationByName(prop.ConfigName)).CustomPropertyManager.Delete(prop.Old_Name) == 0)
            {
                prop.ErrorInfo = "修改成功";
            }
            else
            {
                prop.ErrorInfo = "属性“" + prop.Old_Name + "”不存在";
            }
        }
    }

    public void AddFrame(SwFile swFile, bool isCustomInfo, bool writeModel, bool writeDrw)
    {
        IDrawingDoc drwDoc = (IDrawingDoc)(dynamic)swApp.ActiveDoc;
        dynamic sheetProp = (drwDoc.GetCurrentSheet() as Sheet).GetProperties();
        string frame = null;
        int paperSize = Convert.ToInt32(sheetProp[0]);
        int width = Convert.ToInt32(sheetProp[5] * 1000);
        int height = Convert.ToInt32(sheetProp[6] * 1000);
        frame = paperSize switch
        {
            0 => "a",
            1 => "a",
            2 => "b",
            3 => "c",
            4 => "d",
            6 => "A4",
            7 => "A4V",
            8 => "A3",
            9 => "A2",
            10 => "A1",
            11 => "A0",
            _ => width + "*" + height,
        };
        SwProperty prop = swFile.listSwProperty[0];
        prop.New_Value = frame;
        if (writeDrw)
        {
            ModelDoc2 swDoc3 = (ModelDoc2)(dynamic)swApp.ActiveDoc;
            prop.ConfigName = "自定义属性";
            if (!swDoc3.AddCustomInfo2(prop.New_Name, prop.Type, prop.New_Value.ToString()))
            {
                ((IModelDoc2)swDoc3).set_CustomInfo(prop.New_Name, prop.New_Value.ToString());
            }
            prop.ErrorInfo = "修改成功";
        }
        if (!writeModel)
        {
            return;
        }
        View swView = (dynamic)drwDoc.GetFirstView();
        while (swView.Type < 4)
        {
            swView = (dynamic)swView.GetNextView();
        }
        ModelDoc2 swDoc2 = swView.ReferencedDocument;
        if (swDoc2 != null)
        {
            if (isCustomInfo)
            {
                prop.ConfigName = "自定义属性";
                if (!swDoc2.AddCustomInfo2(prop.New_Name, prop.Type, prop.New_Value.ToString()))
                {
                    ((IModelDoc2)swDoc2).set_CustomInfo(prop.New_Name, prop.New_Value.ToString());
                }
                prop.ErrorInfo = "修改成功";
            }
            else
            {
                Configuration config = (dynamic)swDoc2.GetActiveConfiguration();
                prop.ConfigName = config.Name;
                CustomPropertyManager swCusPropMgr = config.CustomPropertyManager;
                if (swCusPropMgr.Add2(prop.New_Name, prop.Type, prop.New_Value.ToString()) != 0)
                {
                    swCusPropMgr.Set(prop.New_Name, prop.New_Value.ToString());
                }
                prop.ErrorInfo = "修改成功";
            }
            swDoc2.Save();
        }
        else
        {
            prop.ErrorInfo = "找不到引用模型";
        }
    }

    public void AddDrwNum(SwFile swFile, bool writeCus, bool writeConfig, bool writeModel, bool writeDrw)
    {
        if (writeDrw)
        {
            ModelDoc2 swDoc3 = (ModelDoc2)(dynamic)swApp.ActiveDoc;
            if (swDoc3 == null)
            {
                swFile.EditResult = "修改失败：找不到图纸";
                return;
            }
            if (!swDoc3.AddCustomInfo2(swFile.DrwNumName, 30, swFile.DrwNumValue))
            {
                ((IModelDoc2)swDoc3).set_CustomInfo(swFile.DrwNumName, swFile.DrwNumValue);
            }
            swFile.EditResult = "已修改";
        }
        if (!writeModel)
        {
            return;
        }
        DrawingDoc drwDoc = (DrawingDoc)(dynamic)swApp.ActiveDoc;
        if (drwDoc == null)
        {
            swFile.EditResult = "修改失败：找不到图纸";
            return;
        }
        View swView = (dynamic)drwDoc.GetFirstView();
        ModelDoc2 swDoc2 = swView.ReferencedDocument;
        while (swDoc2 == null || swView == null)
        {
            swView = (dynamic)swView.GetNextView();
            swDoc2 = swView.ReferencedDocument;
        }
        if (swDoc2 != null)
        {
            if (writeCus && !swDoc2.AddCustomInfo2(swFile.DrwNumName, 30, swFile.DrwNumValue))
            {
                ((IModelDoc2)swDoc2).set_CustomInfo(swFile.DrwNumName, swFile.DrwNumValue);
            }
            if (writeConfig)
            {
                CustomPropertyManager swCusPropMgr = ((Configuration)(dynamic)swDoc2.GetActiveConfiguration()).CustomPropertyManager;
                if (swCusPropMgr.Add2(swFile.DrwNumName, 30, swFile.DrwNumValue) != 0)
                {
                    swCusPropMgr.Set(swFile.DrwNumName, swFile.DrwNumValue);
                }
            }
            swFile.EditResult = "已修改";
            swDoc2.Save();
        }
        else
        {
            swFile.EditResult = "修改失败：找不到引用模型";
        }
    }

    public bool HasLightWeightComponent()
    {
        return ((AssemblyDoc)swDoc).GetLightWeightComponentCount() > 0;
    }

    public void ResolveAllLightweight()
    {
        ((AssemblyDoc)swDoc).ResolveAllLightweight();
    }

    internal List<string> GetRefFiles(string asmfile)
    {
        if (swApp == null)
        {
            swApp = ConnectSW.iSwApp;
        }
        List<string> asmFilels = new List<string>();
        string[] reffiles = (dynamic)swApp.GetDocumentDependencies(asmfile, 1, 1);
        for (int i = 1; i < reffiles.Length; i += 2)
        {
            string ex = Path.GetExtension(reffiles[i]).ToUpper();
            if (ex == ".SLDASM" || ex == ".SLDPRT")
            {
                asmFilels.Add(reffiles[i]);
            }
        }
        return asmFilels;
    }

    /// <summary>
    /// 添加自定义属性
    /// </summary>
    /// <param name="swFile"></param>
    /// <param name="skip"></param>
    public void AddCustomProperty(SwFile swFile, int skip)
    {
        string fileName = swFile.Name;
        string NameWithoutExt = swFile.NameWithoutExtension; //不带后缀的文件名
        swDoc = (ModelDoc2)(dynamic)swApp.ActiveDoc;
        ICustomPropertyManager swCusPropMgr = ((IModelDocExtension)swDoc.Extension).get_CustomPropertyManager("");
        _ = swDoc.Extension;
        string[] propertyNames = (string[])(dynamic)swCusPropMgr.GetNames();
        foreach (SwAddProperty item in swFile.listSwAddProperty)
        {
            string propertyName = item.PropertyName;
            string rule = item.Rule;
            string propertyValue = "";
            string fileName2 = NameWithoutExt;
            string delimiter = ExtractDelimiterFromRule(rule); //提取分隔符

            if (rule.Contains($"文件名[{delimiter}]"))
            {
                string[] splitResult = fileName.Split(delimiter[0]);
                string[] splitResult2 = fileName2.Split(delimiter[0]);
                if (splitResult2.Length >= 1)
                {
                    int? number = ExtractNumberFromRule(rule);
                    propertyValue = splitResult2[(int)number-1];
                }
                swCusPropMgr.Add2(propertyName, 30, propertyValue);
            }
            else
            {
                switch (rule)
                {
                    case "$文件名中的代号":
                        {
                            string[] splitResult = fileName.Split('-');
                            propertyValue = ((splitResult.Length <= 1) ? splitResult[0] : splitResult[0]);
                            break;
                        }
                    case "$文件名中的名称":
                        {
                            string[] splitResult = fileName.Split('-');
                            propertyValue = ((splitResult.Length != 1) ? fileName.Split('.')[0] : splitResult[0].Split('.')[0]);
                            break;
                        }
                    case "$文件名":
                        propertyValue = fileName.Split('.')[0];
                        break;
                    case "$密度":
                        propertyValue = "\"SW-Density@" + fileName + "\"";
                        break;
                    case "$材料":
                        propertyValue = "\"SW-Material@" + fileName + "\"";
                        break;
                    case "$体积":
                        propertyValue = "\"SW-Volume@" + fileName + "\"";
                        break;
                    case "$表面积":
                        propertyValue = "\"SW-SurfaceArea@" + fileName + "\"";
                        break;
                    case "$配置名":
                        propertyValue = "";
                        break;
                    case "$短日期":
                        DateTime.Now.ToString("yyyy-MM-dd");
                        break;
                    case "$PRP:'SW - 文件名称(File Name)'":
                        propertyValue = "$PRP:\"SW-文件名称(File Name)\"";
                        break;
                    case "$PRP:'SW - Configuration Name'":
                        propertyValue = "$PRP:\"SW-Configuration Name\"";
                        break;
                    case "$PRP:'SW - Author'":
                        propertyValue = "$PRP:\"SW-Author\"";
                        break;
                    case "$PRP:'SW - Title'":
                        propertyValue = "$PRP:\"SW-Title\"";
                        break;
                    case "$PRP:'SW - Created Date'":
                        propertyValue = "$PRP:\"SW-Created Date\"";
                        break;
                    case "$PRP:'SW - Last Saved Date'":
                        propertyValue = "$PRP:\"SW-Last Saved Date\"";
                        break;
                    default:
                        propertyValue = rule;
                        break;
                }
            }
            int result = 0;
            if (skip == 1)
            {
                if (propertyNames != null && propertyNames.Contains(propertyName))
                {
                    item.AddResult = "已跳过";
                    continue;
                }
            }
            else if (propertyNames != null && propertyNames.Contains(propertyName))
            {
                result = swCusPropMgr.Set(propertyName, propertyValue);
                if (result != -1)
                {
                    item.AddResult = "覆盖成功";
                }
                else
                {
                    item.AddResult = "覆盖失败";
                }
                continue;
            }
            result = ((!(propertyName == "$短日期")) ? swCusPropMgr.Add2(propertyName, 30, propertyValue) : swCusPropMgr.Add2(propertyName, 64, propertyValue));
            if (result != -1)
            {
                item.AddResult = "添加成功";
            }
            else
            {
                item.AddResult = "添加失败";
            }
        }
    }


    /// <summary>
    /// 添加配置属性
    /// </summary>
    /// <param name="swFile"></param>
    /// <param name="skip"></param>
    public void AddConfigProperty(SwFile swFile, int skip)
    {
        string fileName = swFile.Name;
        string NameWithoutExt = swFile.NameWithoutExtension; //不带后缀的文件名
        swDoc = (ModelDoc2)(dynamic)swApp.ActiveDoc;
        IConfiguration config = (IConfiguration)(dynamic)swDoc.GetActiveConfiguration();
        ICustomPropertyManager swCusPropMgr = config.CustomPropertyManager;
        _ = swDoc.Extension;
        string[] propertyNames = (string[])(dynamic)swCusPropMgr.GetNames();
        foreach (SwAddProperty item in swFile.listSwAddProperty)
        {
            string propertyName = item.PropertyName;
            string rule = item.Rule;
            string propertyValue = "";
            string fileName2 = NameWithoutExt;
            string delimiter = ExtractDelimiterFromRule(rule); //提取分隔符

            if (rule.Contains($"文件名[{delimiter}]"))
            {
                string[] splitResult = fileName.Split('-');
                string[] splitResult2 = fileName2.Split(delimiter[0]);
                if (splitResult.Length >= 1)
                {
                    int? number = ExtractNumberFromRule(rule);
                    propertyValue = splitResult2[(int)number - 1];
                }
                swCusPropMgr.Add2(propertyName, 30, propertyValue);
            }
            else
            {
                switch (rule)
                {
                    case "$文件名中的代号":
                        {
                            string[] splitResult = fileName.Split('-');
                            propertyValue = ((splitResult.Length <= 1) ? splitResult[0] : splitResult[0]);
                            break;
                        }
                    case "$文件名中的名称":
                        {
                            string[] splitResult = fileName.Split('-');
                            propertyValue = ((splitResult.Length != 1) ? fileName.Split('.')[0] : splitResult[0].Split('.')[0]);
                            break;
                        }
                    case "$文件名":
                        propertyValue = fileName.Split('.')[0];
                        break;
                    case "$密度":
                        propertyValue = "\"SW-Density@" + fileName + "\"";
                        break;
                    case "$材料":
                        propertyValue = "\"SW-Material@" + fileName + "\"";
                        break;
                    case "$体积":
                        propertyValue = "\"SW-Volume@" + fileName + "\"";
                        break;
                    case "$表面积":
                        propertyValue = "\"SW-SurfaceArea@" + fileName + "\"";
                        break;
                    case "$配置名":
                        propertyValue = config.Name;
                        break;
                    case "$短日期":
                        DateTime.Now.ToString("yyyy-MM-dd");
                        break;
                    case "$PRP:'SW - 文件名称(File Name)'":
                        propertyValue = "$PRP:\"SW-文件名称(File Name)\"";
                        break;
                    case "$PRP:'SW - Configuration Name'":
                        propertyValue = "$PRP:\"SW-Configuration Name\"";
                        break;
                    case "$PRP:'SW - Author'":
                        propertyValue = "$PRP:\"SW-Author\"";
                        break;
                    case "$PRP:'SW - Title'":
                        propertyValue = "$PRP:\"SW-Title\"";
                        break;
                    case "$PRP:'SW - Created Date'":
                        propertyValue = "$PRP:\"SW-Created Date\"";
                        break;
                    case "$PRP:'SW - Last Saved Date'":
                        propertyValue = "$PRP:\"SW-Last Saved Date\"";
                        break;
                    default:
                        propertyValue = rule;
                        break;
                }
            }
            int result = 0;
            if (skip == 1)
            {
                if (propertyNames != null && propertyNames.Contains(propertyName))
                {
                    item.AddResult = "已跳过";
                    continue;
                }
            }
            else if (propertyNames != null && propertyNames.Contains(propertyName))
            {
                result = swCusPropMgr.Set(propertyName, propertyValue);
                if (result != -1)
                {
                    item.AddResult = "覆盖成功";
                }
                else
                {
                    item.AddResult = "覆盖失败";
                }
                continue;
            }
            result = ((!(propertyName == "$短日期")) ? swCusPropMgr.Add2(propertyName, 30, propertyValue) : swCusPropMgr.Add2(propertyName, 64, propertyValue));
            if (result != -1)
            {
                item.AddResult = "添加成功";
            }
            else
            {
                item.AddResult = "添加失败";
            }
        }
    }

    /// <summary>
    /// 添加所有配置属性
    /// </summary>
    /// <param name="swFile">SwFile对象</param>
    /// <param name="skip">是否跳过已存在的属性</param>
    public void AddAllConfigProperty(SwFile swFile, int skip)
    {
        string fileName = swFile.Name; // 文件名
        string NameWithoutExt = swFile.NameWithoutExtension; // 不带后缀的文件名
        swDoc = (ModelDoc2)(dynamic)swApp.ActiveDoc; // 激活的文档
        string[] array = (string[])(dynamic)swDoc.GetConfigurationNames(); // 获取所有配置名
        foreach (string configName in array)
        {
            ICustomPropertyManager swCusPropMgr = ((IModelDocExtension)swDoc.Extension).get_CustomPropertyManager(configName);// 获取配置属性管理器
            _ = swDoc.Extension;
            string[] propertyNames = (string[])(dynamic)swCusPropMgr.GetNames(); // 获取所有属性名
            foreach (SwAddProperty item in swFile.listSwAddProperty)
            {
                string propertyName = item.PropertyName;
                string rule = item.Rule;
                string propertyValue = "";
                string delimiter = ExtractDelimiterFromRule(rule); //提取分隔符
                if (rule.Contains($"文件名[{delimiter}]"))
                {
                    string[] splitResult = fileName.Split('-');
                    string[] splitResult2 = NameWithoutExt.Split(delimiter[0]);
                    if (splitResult2.Length >= 1)
                    {
                        int? number = ExtractNumberFromRule(rule);
                        propertyValue = splitResult2[(int)number - 1];
                    }
                    swCusPropMgr.Add2(propertyName, 30, propertyValue);
                }
                else
                {
                    switch (rule)
                    {
                        case "$文件名中的代号":
                            {
                                string[] splitResult = fileName.Split('-');
                                propertyValue = ((splitResult.Length <= 1) ? splitResult[0] : splitResult[0]);
                                break;
                            }
                        case "$文件名中的名称":
                            {
                                string[] splitResult = fileName.Split('-');
                                propertyValue = ((splitResult.Length != 1) ? fileName.Split('.')[0] : splitResult[0].Split('.')[0]);
                                break;
                            }
                        case "$文件名[-][1]":
                            {
                                string[] splitResult = fileName.Split('-');
                                propertyValue = ((splitResult.Length <= 1) ? splitResult[0] : splitResult[0]);
                                break;
                            }
                        case "$文件名[-][2]":
                            {
                                string[] splitResult = fileName.Split('-');
                                propertyValue = ((splitResult.Length <= 1) ? splitResult[0] : splitResult[0]);
                                break;
                            }

                        case "$文件名":
                            propertyValue = fileName.Split('.')[0];
                            break;
                        case "$密度":
                            propertyValue = "\"SW-Density@" + fileName + "\"";
                            break;
                        case "$材料":
                            propertyValue = "\"SW-Material@" + fileName + "\"";
                            break;
                        case "$体积":
                            propertyValue = "\"SW-Volume@" + fileName + "\"";
                            break;
                        case "$表面积":
                            propertyValue = "\"SW-SurfaceArea@" + fileName + "\"";
                            break;
                        case "$配置名":
                            propertyValue = configName;
                            break;
                        case "$短日期":
                            propertyValue = DateTime.Now.ToString("yyyy-MM-dd");
                            break;
                        case "$PRP:'SW - 文件名称(File Name)'":
                            propertyValue = "$PRP:\"SW-文件名称(File Name)\"";
                            break;
                        case "$PRP:'SW - Configuration Name'":
                            propertyValue = "$PRP:\"SW-Configuration Name\"";
                            break;
                        case "$PRP:'SW - Author'":
                            propertyValue = "$PRP:\"SW-Author\"";
                            break;
                        case "$PRP:'SW - Title'":
                            propertyValue = "$PRP:\"SW-Title\"";
                            break;
                        case "$PRP:'SW - Created Date'":
                            propertyValue = "$PRP:\"SW-Created Date\"";
                            break;
                        case "$PRP:'SW - Last Saved Date'":
                            propertyValue = "$PRP:\"SW-Last Saved Date\"";
                            break;
                        default:
                            propertyValue = rule;
                            break;
                    }
                }
                int result = 0;
                if (skip == 1)
                {
                    if (propertyNames != null && propertyNames.Contains(propertyName))
                    {
                        item.AddResult = "已跳过";
                        continue;
                    }
                }
                else if (propertyNames != null && propertyNames.Contains(propertyName))
                {
                    result = swCusPropMgr.Set(propertyName, propertyValue);
                    if (result != -1)
                    {
                        item.AddResult = "覆盖成功";
                    }
                    else
                    {
                        item.AddResult = "覆盖失败";
                    }
                    continue;
                }
                result = ((!(propertyName == "$短日期")) ? swCusPropMgr.Add2(propertyName, 30, propertyValue) : swCusPropMgr.Add2(propertyName, 64, propertyValue));
                if (result != -1)
                {
                    item.AddResult = "添加成功";
                }
                else
                {
                    item.AddResult = "添加失败";
                }
            }
        }
    }
    public void ProcessRuleMid(string rule, string delimiter, string fileName, CustomPropertyManager swCusPropMgr, string propertyName)
    {
        string rulePattern = $"文件名[{delimiter}]";
        if (rule.Contains(rulePattern))
        {
            string[] splitResult = fileName.Split(new string[] { delimiter }, StringSplitOptions.None);
            if (splitResult.Length >= 1)
            {
                string propertyValue = splitResult[0];
                swCusPropMgr.Add2(propertyName, 30, propertyValue);
            }
        }
    }

    public string ProcessRuleLast(string rule, string LastNum, string fileName, string propertyName)
    {
        string rulePattern = $"文件名[-][{LastNum}]";
        if (rule.Contains(rulePattern))
        {
            string[] splitResult = fileName.Split('-');
            if (splitResult.Length >= 1)
            {
                for (int i = 0; i < splitResult.Length - 1; i++)
                {
                    splitResult[0] += "-" + splitResult[i + 1];
                }
                return splitResult[0];
            }
        }
        return null;
    }

    public int? ExtractNumberFromRule(string rule)
    {
        var match = Regex.Match(rule, @"文件名\[[_ ]\]\[(\d+)\]");
        if (match.Success)
        {
            return int.Parse(match.Groups[1].Value);
        }
        return null;
    }

    public string ExtractDelimiterFromRule(string rule)
    {
        var match = Regex.Match(rule, @"文件名\[([_ ])\]\[\d+\]");
        if (match.Success)
        {
            return match.Groups[1].Value;
        }
        return null;
    }
}
