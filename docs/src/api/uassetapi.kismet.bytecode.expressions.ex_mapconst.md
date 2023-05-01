# EX_MapConst

Namespace: UAssetAPI.Kismet.Bytecode.Expressions

A single Kismet bytecode instruction, corresponding to the [EExprToken.EX_MapConst](./uassetapi.kismet.bytecode.eexprtoken.md#ex_mapconst) instruction.

```csharp
public class EX_MapConst : UAssetAPI.Kismet.Bytecode.KismetExpression
```

Inheritance [Object](https://docs.microsoft.com/en-us/dotnet/api/system.object) → [KismetExpression](./uassetapi.kismet.bytecode.kismetexpression.md) → [EX_MapConst](./uassetapi.kismet.bytecode.expressions.ex_mapconst.md)

## Fields

### **KeyProperty**

Pointer to this constant's key property (FProperty*).

```csharp
public KismetPropertyPointer KeyProperty;
```

### **ValueProperty**

Pointer to this constant's value property (FProperty*).

```csharp
public KismetPropertyPointer ValueProperty;
```

### **Elements**

Set constant entries.

```csharp
public KismetExpression[] Elements;
```

### **Tag**

An optional tag which can be set on any expression in memory. This is for the user only, and has no bearing in the API itself.

```csharp
public object Tag;
```

### **RawValue**

```csharp
public object RawValue;
```

## Properties

### **Token**

The token of this expression.

```csharp
public EExprToken Token { get; }
```

#### Property Value

[EExprToken](./uassetapi.kismet.bytecode.eexprtoken.md)<br>

### **Inst**

The token of this instruction expressed as a string.

```csharp
public string Inst { get; }
```

#### Property Value

[String](https://docs.microsoft.com/en-us/dotnet/api/system.string)<br>

## Constructors

### **EX_MapConst()**

```csharp
public EX_MapConst()
```

## Methods

### **Read(AssetBinaryReader)**

Reads out the expression from a BinaryReader.

```csharp
public void Read(AssetBinaryReader reader)
```

#### Parameters

`reader` [AssetBinaryReader](./uassetapi.assetbinaryreader.md)<br>
The BinaryReader to read from.

### **Write(AssetBinaryWriter)**

Writes the expression to a BinaryWriter.

```csharp
public int Write(AssetBinaryWriter writer)
```

#### Parameters

`writer` [AssetBinaryWriter](./uassetapi.assetbinarywriter.md)<br>
The BinaryWriter to write from.

#### Returns

[Int32](https://docs.microsoft.com/en-us/dotnet/api/system.int32)<br>
The iCode offset of the data that was written.