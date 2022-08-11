from opentap import *
import numpy as np
import System
from System import Array, String
from System.Collections.Generic import List
from System.Runtime.InteropServices import GCHandle, GCHandleType
import ctypes

map_np_to_net = {
   np.dtype(np.float32): System.Single,
   np.dtype(np.float64): System.Double,
   np.dtype(np.int8)   : System.SByte,
   np.dtype(np.int16)  : System.Int16,
   np.dtype(np.int32)  : System.Int32,
   np.dtype(np.int64)  : System.Int64,
   np.dtype(np.uint8)  : System.Byte,
   np.dtype(np.uint16) : System.UInt16,
   np.dtype(np.uint32) : System.UInt32,
   np.dtype(np.uint64) : System.UInt64,
   np.dtype(np.bool)   : System.Boolean,
}

def toNetArrayFast(npArray):
   dims = npArray.shape
   dtype = npArray.dtype

   if not npArray.flags.c_contiguous or not npArray.flags.aligned:
      npArray = np.ascontiguousarray(npArray)
   try:
      netArray = Array.CreateInstance(map_np_to_net[dtype], *dims)
   except KeyError:
      raise NotImplementedError(f'asNetArray does not yet support dtype {dtype}')

   try:
      destHandle = GCHandle.Alloc(netArray, GCHandleType.Pinned)
      sourcePtr = npArray.__array_interface__['data'][0]
      destPtr = destHandle.AddrOfPinnedObject().ToInt64()
      ctypes.memmove(destPtr, sourcePtr, npArray.nbytes)
   finally:
      if destHandle.IsAllocated:
         destHandle.Free()
   return netArray

@attribute(OpenTap.Display("Numpy Step", "An example of using numpy to generate results.", "Python Example"))
class NumpyStep(TestStep):
   """
   This step calls numpy and generates results in an 'efficient' way. 
   """
   Points = property(System.Int32, 32)
   def __init__(self):
      super().__init__()
   def Run(self):
      super().Run()
      
      # Use numpy to generate some data.
      X = np.arange(self.Points)
      Y = np.sin(X)
      
      # It is also possible to loop across all the values of the arrays, but this is significantly faster.
      xx = toNetArrayFast(X)
      yy = toNetArrayFast(Y)

      columnNames = List[String]()
      columnNames.Add("X")
      columnNames.Add("Y")
      
      # Finally publish the arrays as results
      self.Results.PublishTable("XY", columnNames, xx, yy)
      self.log.Info("Generated {0}x2 Points", self.Points)
      
