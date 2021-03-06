﻿// originally published by Julien
// original implementation taken from http://lepensemoi.free.fr/index.php/2010/02/05/binary-random-access-list
//J.F. modified

namespace FSharpx.DataStructures

open System.Collections
open System.Collections.Generic

type TreeBRAL<'a> =
    | Leaf of 'a
    | Node of int * TreeBRAL<'a> * TreeBRAL<'a>

type Digit<'a> =
    | Zero
    | One of TreeBRAL<'a>

type BinaryRandomAccessList<'a> (randomAccessList) =

    member this.randomAccessList = randomAccessList

    static member private size : TreeBRAL<'a> -> int = function
        | Leaf _ -> 1
        | Node (w, _, _) -> w

    static member private length : int * int * list<Digit<'a>> -> int = function
        | len, acc, [] -> len
        | len, acc, One(_)::ts -> BinaryRandomAccessList.length ((len + acc), (2 * acc), ts)
        | len, acc, Zero::ts -> BinaryRandomAccessList.length (len, (2 * acc), ts)

    static member private link (t1:TreeBRAL<'a>) (t2:TreeBRAL<'a>) = Node(BinaryRandomAccessList.size t1 + BinaryRandomAccessList.size t2, t1, t2)

    static member private lookupTree : int * TreeBRAL<'a> -> 'a = function
        | 0, Leaf x -> x
        | i, Leaf x -> raise Exceptions.OutOfBounds
        | i, Node(w, t1, t2) ->
            if i < w / 2 then BinaryRandomAccessList.lookupTree (i, t1) else BinaryRandomAccessList.lookupTree (i - w/2, t2)

    static member private tryLookupTree : int * TreeBRAL<'a> -> 'a option = function
        | 0, Leaf x -> Some(x)
        | i, Leaf x -> None
        | i, Node(w, t1, t2) ->
            if i < w / 2 then BinaryRandomAccessList.tryLookupTree (i, t1) else BinaryRandomAccessList.tryLookupTree (i - w/2, t2)

    static member private lookup : int * list<Digit<'a>> -> 'a = function
        | _, [] -> raise Exceptions.OutOfBounds
        | i, Zero::ts -> BinaryRandomAccessList.lookup (i, ts)
        | i, One t::ts ->
            let size = BinaryRandomAccessList.size t
            if i < size then BinaryRandomAccessList.lookupTree (i, t) else BinaryRandomAccessList.lookup ((i - size), ts)

    static member private tryLookup : int * list<Digit<'a>> -> 'a option = function
        | _, [] -> None
        | i, Zero::ts -> BinaryRandomAccessList.tryLookup (i, ts)
        | i, One t::ts ->
            let size = BinaryRandomAccessList.size t
            if i < size then BinaryRandomAccessList.tryLookupTree (i, t) else BinaryRandomAccessList.tryLookup ((i - size), ts)

    static member private consTree  : TreeBRAL<'a> * list<Digit<'a>> ->  list<Digit<'a>> = function
        | t, [] -> [One t]
        | t, Zero::ts -> One t :: ts
        | t1, One t2 :: ts -> Zero :: BinaryRandomAccessList.consTree ((BinaryRandomAccessList.link t1 t2), ts) 

    static member private unconsTree : list<Digit<'a>> -> TreeBRAL<'a> * list<Digit<'a>> = function
        | [] -> raise Exceptions.Empty
        | [One t] -> t, []
        | One t :: ts -> t, Zero::ts
        | Zero::ts ->
            match BinaryRandomAccessList.unconsTree ts with
            | Node(_, t1, t2), ts' -> t1, One t2::ts'
            | _ -> failwith "should never get there"

    static member private tryUnconsTree : list<Digit<'a>> -> (TreeBRAL<'a> * list<Digit<'a>>) option = function
        | [] -> None
        | [One t] -> Some(t, [])
        | One t :: ts -> Some(t, Zero::ts)
        | Zero::ts ->
            match BinaryRandomAccessList.unconsTree ts with
            | Node(_, t1, t2), ts' -> Some(t1, One t2::ts')
            | _ -> None
 
    static member updateTree : int * 'a * TreeBRAL<'a> -> TreeBRAL<'a> = function
        | 0, y, Leaf x -> Leaf y
        | i, y, Leaf x -> raise Exceptions.OutOfBounds
        | i, y, Node(w, t1, t2) ->
            if i < w / 2 then
              Node(w, BinaryRandomAccessList.updateTree (i, y, t1) , t2)
            else
              Node(w, t1, BinaryRandomAccessList.updateTree (i - w/2, y, t2))

    static member tryUpdateTree : int * 'a * TreeBRAL<'a> -> TreeBRAL<'a> option = function
        | 0, y, Leaf x -> Some(Leaf y)
        | i, y, Leaf x -> None
        | i, y, Node(w, t1, t2) ->
            if i < w / 2 then
              Some(Node(w, BinaryRandomAccessList.updateTree (i, y, t1) , t2))
            else
              Some(Node(w, t1, BinaryRandomAccessList.updateTree (i - w/2, y, t2)))

    static member update i y : list<Digit<'a>> -> list<Digit<'a>> = function
        | []  -> raise Exceptions.OutOfBounds
        | Zero::ts -> Zero :: BinaryRandomAccessList.update i y ts
        | One t::ts ->
            let size = BinaryRandomAccessList.size t
            if i < size then
              let a = One <| BinaryRandomAccessList.updateTree (i, y, t)
              a :: ts
            else
              (One t) :: BinaryRandomAccessList.update (i - size) y ts

    static member tryUpdate i y : list<Digit<'a>> -> list<Digit<'a>> option = function
        | []  -> None
        | Zero::ts -> Some(Zero :: BinaryRandomAccessList.update i y ts)
        | One t::ts ->
            let size = BinaryRandomAccessList.size t
            if i < size then
              let a = One <| BinaryRandomAccessList.updateTree (i, y, t)
              Some(a :: ts)
            else
              Some((One t) :: BinaryRandomAccessList.update (i - size) y ts)

    static member internal ofSeq (s:seq<'a>) : BinaryRandomAccessList<'a> = 
        if Seq.isEmpty s then BinaryRandomAccessList([])
        else
            let a = Array.ofSeq s
            let rec loop (acc: list<Digit<'a>>) dec (a': array<'a>) =
                if dec < 0 then BinaryRandomAccessList(acc)
                else loop (BinaryRandomAccessList.consTree ((Leaf a'.[dec]), acc)) (dec - 1) a'

            loop [] (a.Length - 1) a

    member this.Cons (x:'a)  = BinaryRandomAccessList(BinaryRandomAccessList.consTree ((Leaf x), randomAccessList))

    member this.Head() =
        match BinaryRandomAccessList.unconsTree randomAccessList with
        | Leaf x, _ -> x
        | _ -> raise Exceptions.Empty

    member this.TryGetHead() =
       match BinaryRandomAccessList.tryUnconsTree randomAccessList with
        | Some(Leaf x, _) -> Some(x)
        | _ -> None

    member this.IsEmpty() = 
        match randomAccessList with
        | [] -> true
        | _ -> false

    member this.Length() = BinaryRandomAccessList.length (0, 1, randomAccessList)

    member this.Lookup (i:int) = BinaryRandomAccessList.lookup (i, randomAccessList)

    member thie.TryLookup (i:int) = BinaryRandomAccessList.tryLookup (i, randomAccessList)

    member this.Rev() =

        let rec loop : list<Digit<'a>> * list<Digit<'a>> -> BinaryRandomAccessList<'a>  = function
            | acc, [] -> BinaryRandomAccessList(acc)  
            | acc, ral -> 
                let x, ts = BinaryRandomAccessList.unconsTree ral
                loop ((BinaryRandomAccessList.consTree (x, acc)), ts)
            
        loop ([], randomAccessList)

    member this.Tail() =
        let _, ts = BinaryRandomAccessList.unconsTree randomAccessList
        BinaryRandomAccessList(ts)

    member this.TryGetTail() =
        match BinaryRandomAccessList.tryUnconsTree randomAccessList with
        | None -> None
        | Some(_, xs) -> Some(BinaryRandomAccessList(xs))

    member this.Uncons() =
        match BinaryRandomAccessList.unconsTree randomAccessList with
        | (Leaf x), ts -> x, BinaryRandomAccessList(ts)
        | _ -> failwith "can't get there"

    member this.TryUncons() =
        match BinaryRandomAccessList.tryUnconsTree randomAccessList with
        | Some((Leaf x), ts) -> Some(x, BinaryRandomAccessList(ts))
        | _ -> None

    member this.Update i y = BinaryRandomAccessList(BinaryRandomAccessList.update i y randomAccessList)
        
    member this.TryUpdate i y =
        match BinaryRandomAccessList.tryUpdate i y randomAccessList with
        | None -> None
        | Some(ts) -> Some(BinaryRandomAccessList(ts))

    interface IRandomAccessList<'a> with

        member this.Cons (x : 'a) = this.Cons x :> _

        member this.Count() = this.Length()

        member this.Head() = this.Head()

        member this.TryGetHead() = this.TryGetHead()

        member this.IsEmpty() = this.IsEmpty()

        member this.Length() = this.Length()

        member this.Lookup i = this.Lookup i

        member this.TryLookup i = this.TryLookup i

        member this.Rev() = this.Rev() :> _

        member this.Tail() = this.Tail() :> _

        member this.TryGetTail() =
            match this.TryGetTail() with
            | None -> None
            | Some(xs) -> Some(xs :> _)

        member this.Uncons() = 
            let x, ts = this.Uncons() 
            x, ts :> _

        member this.TryUncons() =
            match this.TryUncons()  with
            | None -> None
            | Some(x, ts) -> Some(x, ts :> _)

        member this.Update i y = this.Update i y :> _
        
        member this.TryUpdate i y =
            match this.TryUpdate i y with
            | None -> None
            | Some(ts) -> Some(ts :> _)

        member this.GetEnumerator() = 
            let e = seq {
                match this.TryUncons() with
                | None -> () 
                | Some(x, ts) ->
                    yield x 
                    yield! ts}
            e.GetEnumerator()

        member this.GetEnumerator() = (this :> _ seq).GetEnumerator() :> IEnumerator  

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module BinaryRandomAccessList =   
    //pattern discriminator

    let (|Cons|Nil|) (l: BinaryRandomAccessList<'a>) = match l.TryUncons() with Some(a,b) -> Cons(a,b) | None -> Nil
  
     ///returns a new random access list with the element added to the beginning
    let inline cons x (xs: BinaryRandomAccessList<'a>) = xs.Cons x   
  
    ///returns the first element
    let inline head (xs: BinaryRandomAccessList<'a>)  = xs.Head()

    ///returns option first element 
    let inline tryGetHead (xs: BinaryRandomAccessList<'a>)  = xs.TryGetHead()

    ///returns a empty random access list
    let inline empty() = BinaryRandomAccessList<'a>([])

    ///returns true if the random access list has no elements
    let inline isEmpty (xs: BinaryRandomAccessList<'a>) = xs.IsEmpty()

    ///returns the count of elememts
    let inline length (xs: BinaryRandomAccessList<'a>) = xs.Length() 

    ///returns element by index
    let inline lookup i (xs: BinaryRandomAccessList<'a>) = xs.Lookup i 

    ///returns option element by index
    let inline tryLookup i (xs: BinaryRandomAccessList<'a>) = xs.TryLookup i

    ///returns random access list from the sequence
    let ofSeq s = BinaryRandomAccessList.ofSeq s

    //returns random access list reversed
    let inline rev (xs: BinaryRandomAccessList<'a>) = xs.Rev()

    ///returns a new random access list of the elements trailing the first element
    let inline tail (xs: BinaryRandomAccessList<'a>) = xs.Tail()

    ///returns a option random access list of the elements trailing the first element
    let inline tryGetTail (xs: BinaryRandomAccessList<'a>) = xs.TryGetTail()

    ///returns the first element and tail
    let inline uncons (xs: BinaryRandomAccessList<'a>) = xs.Uncons()

    ///returns the option first element and tail
    let inline tryUncons (xs: BinaryRandomAccessList<'a>) = xs.TryUncons()

    ///returns random access list with element updated by index
    let inline update i y (xs: BinaryRandomAccessList<'a>) = xs.Update i y

    ///returns option random access list with element updated by index
    let inline tryUpdate i y (xs: BinaryRandomAccessList<'a>) = xs.TryUpdate i y