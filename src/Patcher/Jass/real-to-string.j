function R2SWF takes real r, integer width, integer precision returns string
    local string result = I2S(R2I(r))
local real absValue = RAbsBJ(r)
local integer div = 1
   
if precision > 0 then
set result = result + "."
loop
set div = div * 10
set result = result + I2S(ModuloInteger(R2I(absValue / (1.0 / I2R(div))), 10))
set precision = precision - 1
exitwhen precision == 0
endloop
    endif
   
loop
    exitwhen width <= 0
set width = width - 1
exitwhen width < StringLength(result)
set result = " " + result
endloop
   
return result
endfunction

    function R2SF takes real r returns string
return R2SWF(r, 1, 3)
endfunction