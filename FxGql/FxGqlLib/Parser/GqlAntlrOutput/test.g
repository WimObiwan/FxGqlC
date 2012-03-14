grammar test;

options 
{
	language = CSharp2;
	output = AST;
}

tokens
{
	T_PLUS;
	T_OP_BINARY;
	T_OP_UNARY;
}


expression_3a //: expression_4 (WS op_3 WS expression_4)*
	: (a=expression_3b->$a) (WS op_3a WS b=expression_3b -> ^(T_OP_BINARY op_3a $expression_3b $b))*
	;

op_3a	: '+' -> T_PLUS
	;

expression_3b //: (op_7 WS)* expression_8
	: op_3b WS expression_3b -> ^(T_OP_UNARY op_3b expression_3b)
	| NUMBER
	;
/*
expression_3b //: (op_7 WS)* expression_8
	: (op_3b WS -> ^(T_OP_UNARY op_3b $expression_3b))* exp0
	//: (op_3b WS)* NUMBER
	;
*/
op_3b	: '+' -> T_PLUS
	;

exp0	: NUMBER
	| STRING
	| SYSTEMVAR
	;	
	
STRING
	: '\'' ( (~('\'')|'\'''\'')* ) '\''
	;

NUMBER
	: DIGIT+
	;

TOKEN
	: ('A'..'Z' | 'a'..'z' | '_') ('A'..'Z' | 'a'..'z' | '_' | '0'..'9')*
	;

SYSTEMVAR
	: '$' TOKEN
	;

WS
	: (' '|'\t'|'\n'|'\r')+
	;
    
fragment DIGIT	:	'0'..'9';


	
