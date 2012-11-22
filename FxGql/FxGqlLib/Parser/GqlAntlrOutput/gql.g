grammar gql;

options 
{
	language = CSharp2;
	output = AST;
	//k=11;
}

tokens
{
	T_ROOT;
	T_SELECT;
	T_SELECT_UNION;
	T_SELECT_SIMPLE;
	T_ALL;
	T_DISTINCT;
	T_TOP;
	T_BOTTOM;
	T_COLUMNLIST;
	T_EXPRESSIONLIST;
	T_INTO;
	T_FROM;
	T_WHERE;
	T_GROUPBY;
	T_ORDERBY;
	T_FILE;
	T_FILESUBQUERY;
	T_FILEOPTION;
	T_SUBQUERY;
	T_INTEGER;
	T_STRING;
	T_SYSTEMVAR;
	T_FUNCTIONCALL;
	T_OP_UNARY;
	T_OP_BINARY;
	T_EQUAL;
	T_GREATER;
	T_LESS;
	T_NOTGREATER;
	T_NOTLESS;
	T_NOTEQUAL;
	T_PLUS;
	T_NOT;
	T_AND;
	T_OR;
	T_LIKE;
	T_NOTLIKE;
	T_MATCH;
	T_NOTMATCH;
	T_BETWEEN;
	T_NOTBETWEEN;
	T_ORDERBY_COLUMN;
	T_ORDERBY_ASC;
	T_ORDERBY_DESC;
	T_CONVERT;
	T_MINUS;
	T_BITWISE_AND;
	T_BITWISE_OR;
	T_BITWISE_XOR;
	T_BITWISE_NOT;
	T_PRODUCT;
	T_DIVIDE;
	T_MODULO;
	T_IN;
	T_NOTIN;
	T_ANY;
	T_EXISTS;
	T_COLUMN;
	T_CASE;
	T_CASE_WHEN;
	T_CASE_ELSE;
	T_USE;
	T_DECLARE;
	T_DECLARATION;
	T_SET_VARIABLE;
	T_VARIABLE;
	T_CREATE_VIEW;
	T_VIEW_NAME;
	T_VIEW;
	T_ORDERBY_ORIG;
	T_GROUPBY_ORIG;
	T_DROP_VIEW;
	T_DROP_TABLE;
	T_TABLE_ALIAS;
	T_ALLCOLUMNS;
	T_HAVING;
	T_COUNT;
	T_DISTINCTCOUNT;
	T_DATEPART;
	T_SIMPLEPIVOT;
}

@parser::namespace { FxGqlLib }
@lexer::namespace { FxGqlLib }

@lexer::header {
// `XXX' does not need a CLSCompliant attribute because the assembly is not marked as CLS-compliant (CS3021)
#pragma warning disable 3021
// The private field `XXX' is assigned but its value is never used (CS0414)
#pragma warning disable 414
}
@parser::header {
// `XXX' does not need a CLSCompliant attribute because the assembly is not marked as CLS-compliant (CS3021)
#pragma warning disable 3021
// The private field `XXX' is assigned but its value is never used (CS0414)
#pragma warning disable 414
}

@lexer::members { const int HIDDEN = Hidden; }

parse
	: WS? (commands WS?)? EOF
	-> ^(T_ROOT commands?)
	;
	
commands 
	: command (WS? ';' WS? command)* (WS? ';')?
	-> command+
	;
	
command
	: select_command
	| use_command
	| declare_command
	| set_command
	| create_view_command
	| drop_view_command
	| drop_table_command
	;

///////////////////////////////////////////////////////////////////////////////
// SELECT COMMAND

select_command
	: select_command_union (WS orderby_clause)?
		-> ^(T_SELECT select_command_union orderby_clause?)
	;

// select top x ... order by y
// select top x1 ... union select top x2 ... order by y

select_command_union
	: (a=select_command_simple->$a) (WS UNION WS b=select_command_simple -> ^(T_SELECT_UNION $select_command_union $b))*
	;
	
select_command_simple
	: ('(') => subquery
	| SELECT (WS distinct_clause)? (WS top_clause)? (WS bottom_clause)? WS column_list (WS into_clause)? (WS from_clause)? (WS where_clause)? (WS groupby_clause)? (WS having_clause)?
		-> ^(T_SELECT_SIMPLE distinct_clause? top_clause? bottom_clause? column_list into_clause? from_clause? where_clause? groupby_clause? having_clause?)
	;
	
distinct_clause
	: DISTINCT -> T_DISTINCT
	| ALL -> T_ALL
	;
	
top_clause
	: TOP WS expression_atom -> ^(T_TOP expression_atom)
	;
		
bottom_clause
	: BOTTOM WS expression_atom -> ^(T_BOTTOM expression_atom)
	;
		
column_list
	: column (WS? ',' WS? column)*
	-> ^(T_COLUMNLIST column*)
	;
	
column
	: all_columns
	| expression (WS SIMPLE_FILE)? -> ^(T_COLUMN expression SIMPLE_FILE?)
	;

all_columns
	: (table_alias WS? '.' WS?)? '*' -> ^(T_ALLCOLUMNS table_alias?)
	;

into_clause
	: INTO WS file -> ^(T_INTO file)
	;
	
from_clause
	: FROM WS from_clause_item (WS? ',' WS? from_clause_item)* (WS table_alias)? -> ^(T_FROM table_alias? from_clause_item*)
//	| FROM WS from_clause_item (WS? ',' WS? from_clause_item)* (WS table_alias) -> ^(T_FROM table_alias from_clause_item*)
	;
	
from_clause_item
	: STRING -> ^(T_FILE STRING)
	| file
	| (subquery WS SIMPLEPIVOT) => simplepivot
	| subquery
	| view_name (WS? '(' expression_list? ')')? -> ^(T_VIEW view_name expression_list?)
	;
simplepivot
	: subquery WS SIMPLEPIVOT WS '(' WS? column_list WS 
	FOR WS expression_atom WS IN WS '(' WS? expression_list WS? ')' (WS WITH WS '(' WS? with_options WS? ')')? WS? ')'
	-> ^(T_SIMPLEPIVOT subquery column_list expression_atom expression_list with_options?)
	;
	
subquery
	: '(' WS? select_command WS? ')' -> ^(T_SUBQUERY select_command)
	;

file
	: '[' WS? file_spec (WS file_options)? WS? ']' -> ^(T_FILE file_spec file_options?) 
	| '[' WS? subquery WS? ']' -> ^(T_FILESUBQUERY subquery)
	| SIMPLE_FILE -> ^(T_FILE SIMPLE_FILE)
	;

file_spec
	: string
	| variable
	;
	
file_options
	: file_option (WS file_option)* -> file_option* 
	;
	
file_option
	: '-' file_option_name ( WS? '=' WS? file_option_value)? -> ^(T_FILEOPTION file_option_name file_option_value?)
	;

file_option_name
	: TOKEN
	;
	
file_option_value
	: TOKEN | STRING | NUMBER | variable
	;
	
with_options
	: with_option (WS? ',' WS? with_option)*
	;
	
with_option
	: file_option_name ( WS? '=' WS? file_option_value)? -> ^(T_FILEOPTION file_option_name file_option_value?)
	;
	
where_clause
	: WHERE WS expression
	-> ^(T_WHERE expression)
	;
groupby_clause
	: GROUP WS BY WS orderby_column_list
	-> ^(T_GROUPBY orderby_column_list)
	;
	
having_clause
	: HAVING WS expression
	-> ^(T_HAVING expression)
	;
	
orderby_clause
	: ORDER WS BY WS orderby_column_list
	-> ^(T_ORDERBY orderby_column_list)
	;
	
orderby_column_list
	: orderby_column (WS? ',' WS? orderby_column)*
	-> orderby_column*
	;
	
orderby_column
	: expression (WS orderby_direction)? -> ^(T_ORDERBY_COLUMN expression orderby_direction?)
	;
	
orderby_direction
	: ASC -> T_ORDERBY_ASC
	| DESC -> T_ORDERBY_DESC
	| ORIG -> T_ORDERBY_ORIG
	;

///////////////////////////////////////////////////////////////////////////////
// USE COMMAND

use_command
	: USE WS file -> ^(T_USE file)
	;

///////////////////////////////////////////////////////////////////////////////
// DECLARE COMMAND

declare_command
	: DECLARE WS declaration_list -> declaration_list
	;
	
declaration_list 
	: declaration (WS? ',' WS? declaration)*
	-> ^(T_DECLARE declaration+)
	;
	
declaration
	: variable WS (AS WS)? datatype
	-> ^(T_DECLARATION variable datatype)
	;

///////////////////////////////////////////////////////////////////////////////
// VIEW COMMAND

create_view_command
	: CREATE WS VIEW WS view_name (WS? '(' declaration_list ')')? WS AS WS select_command
	-> ^(T_CREATE_VIEW view_name declaration_list? select_command)
	;
	
drop_view_command
	: DROP WS VIEW WS view_name
	-> ^(T_DROP_VIEW view_name)
	;

view_name
	: TOKEN -> ^(T_VIEW_NAME TOKEN)
	;
	
///////////////////////////////////////////////////////////////////////////////
// DROP TABLE COMMAND
drop_table_command
	: DROP WS TABLE WS file
	-> ^(T_DROP_TABLE file)
	;


///////////////////////////////////////////////////////////////////////////////
// DECLARE COMMAND

set_command
	: SET WS variable WS? '=' WS? expression
	-> ^(T_SET_VARIABLE variable expression)
	;

///////////////////////////////////////////////////////////////////////////////
// EXPRESSIONS
// http://msdn.microsoft.com/en-us/library/ms190276.aspx
expression_list
	: expression (WS? ',' WS? expression)* -> ^(T_EXPRESSIONLIST expression+)
	;
	
expression_list_or_select_command
	: (SELECT) => select_command
	| expression_list 
	;
	
expression
	: expression_7
	;
	
expression_7 //: expression_6 (WS op_5 WS expression_6)*
	: (a=expression_6->$a) (WS (
	op_7 WS b=expression_6 -> ^(T_OP_BINARY op_7 $expression_7 $b)
	| IN WS? '(' WS? expression_list_or_select_command WS? ')' -> ^(T_OP_BINARY T_IN $expression_7 expression_list_or_select_command)
	| NOT WS IN WS? '(' WS? (expression_list_or_select_command) WS? ')' -> ^(T_OP_BINARY T_NOTIN $expression_7 expression_list_or_select_command)
	| op_4 WS? (SOME | ANY) WS? '(' expression_list_or_select_command ')' -> ^(T_OP_BINARY T_ANY op_4 $expression_7 expression_list_or_select_command)
	| op_4 WS? ALL WS? '(' expression_list_or_select_command ')' -> ^(T_OP_BINARY T_ALL op_4 $expression_7 expression_list_or_select_command)
	))*
	;

op_7	: OR -> T_OR
	| BETWEEN -> T_BETWEEN
	| NOT WS BETWEEN -> T_NOTBETWEEN
	;

expression_6 //: expression_7 (WS op_6 WS expression_7)*
	: (a=expression_5->$a) (WS op_6 WS b=expression_5 -> ^(T_OP_BINARY op_6 $expression_6 $b))*
	; 
	
op_6	: AND -> T_AND	
	;
	
expression_5 //: (op_7 WS)* expression_8
	: op_5 WS expression_5 -> ^(T_OP_UNARY op_5 expression_5)
	| expression_4
	;

op_5 	: NOT -> T_NOT
	;

expression_4 //: expression_5 (WS op_4 WS expression_5)*
	: (a=expression_3->$a) (WS? op_4 WS? b=expression_3 -> ^(T_OP_BINARY op_4 $expression_4 $b))*
	;

op_4	: '=' -> T_EQUAL
	| '>=' -> T_NOTLESS 
	| '<=' -> T_NOTGREATER 
	| '<>' -> T_NOTEQUAL
	| '!=' -> T_NOTEQUAL
	| '!>' -> T_NOTGREATER
	| '!<' -> T_NOTLESS
	| '>' -> T_GREATER 
	| '<' -> T_LESS
	| LIKE -> T_LIKE
	| NOT WS LIKE -> T_NOTLIKE
	| MATCH -> T_MATCH
	| NOT WS MATCH -> T_NOTMATCH
	;

expression_3 //: expression_4 (WS op_3 WS expression_4)*
	: (a=expression_2->$a) (WS? op_3 WS? b=expression_2 -> ^(T_OP_BINARY op_3 $expression_3 $b))*
	;

op_3	: '+' -> T_PLUS
	| '-' -> T_MINUS
	| '&' -> T_BITWISE_AND
	| '|' -> T_BITWISE_OR
	| '^' -> T_BITWISE_XOR
	;
	
expression_2 //: expression_5 (WS op_4 WS expression_5)*
	: (a=expression_1->$a) (WS? op_2 WS? b=expression_1 -> ^(T_OP_BINARY op_2 $expression_2 $b))*
	;

op_2	: '*' -> T_PRODUCT
	| '/' -> T_DIVIDE
	| '%' -> T_MODULO 
	;

expression_1
	: op_1 WS? expression_1 -> ^(T_OP_UNARY op_1 expression_1)
	| expression_atom
	;

op_1	: '~' -> T_BITWISE_NOT
 	| '+' -> T_PLUS
	| '-' -> T_MINUS
	;

expression_atom
	: number -> ^(T_INTEGER number)
	| string
	| SYSTEMVAR -> ^(T_SYSTEMVAR SYSTEMVAR)
	| variable
	| (subquery) => subquery
	| '(' expression ')' -> expression
	| functioncall_or_column
	| specialfunctioncall
	| case
	| EXISTS WS? '(' WS? select_command WS? ')' -> ^(T_EXISTS select_command)
	;
///////////////////////////////////////////////////////////////////////////////

functioncall_or_column
	: TOKEN WS? '(' WS? (expression WS? (',' WS? expression WS?)*)? ')' -> ^(T_FUNCTIONCALL TOKEN expression*)
	| COUNT WS? '(' WS? expression WS? ')' -> ^(T_FUNCTIONCALL T_COUNT expression)
	| COUNT WS? '(' WS? DISTINCT WS expression WS? ')' -> ^(T_FUNCTIONCALL T_DISTINCTCOUNT expression)
	| COUNT WS? '(' WS? all_columns WS? ')' -> ^(T_FUNCTIONCALL T_COUNT all_columns)
	| COUNT WS? '(' WS? DISTINCT WS all_columns WS? ')' -> ^(T_FUNCTIONCALL T_DISTINCTCOUNT all_columns)
	//| TOKEN -> ^(T_COLUMN TOKEN)
	| (table_alias WS? '.' WS?)? column_name -> ^(T_COLUMN column_name table_alias?)
	;
	
column_name
	: SIMPLE_FILE
	;

table_alias
	: SIMPLE_FILE -> ^(T_TABLE_ALIAS SIMPLE_FILE)
	;
	
specialfunctioncall
	: CONVERT WS? '(' WS? TOKEN WS? ',' WS? expression WS? (',' WS? STRING WS?)? ')' -> ^(T_CONVERT TOKEN expression STRING?)
	| CAST WS? '(' WS? expression WS AS WS datatype WS? ')' -> ^(T_CONVERT TOKEN expression)
	| DATEADD WS? '(' WS? datepart WS? ',' WS? expression WS? ',' WS? expression WS? ')' -> ^(T_FUNCTIONCALL DATEADD datepart expression+)
	| DATEDIFF WS? '(' WS? datepart WS? ',' WS? expression WS? ',' WS? expression WS? ')' -> ^(T_FUNCTIONCALL DATEDIFF datepart expression+)
	| DATEPART WS? '(' WS? datepart WS? ',' WS? expression WS? ')' -> ^(T_FUNCTIONCALL DATEPART datepart expression)
	;

datepart 
	: TOKEN -> ^(T_DATEPART TOKEN)
	;
	
number 	: NUMBER;

string	: STRING -> ^(T_STRING STRING)
	;
	
datatype
	: TOKEN
	;
	
variable
	: VARIABLE -> ^(T_VARIABLE VARIABLE)
	;
	
case
	: CASE WS (expression WS)? (case_when WS)* (case_else WS)? END
	-> ^(T_CASE expression? case_when* case_else?)
	;
	
case_when
	: WHEN WS a=expression WS THEN WS b=expression
	-> ^(T_CASE_WHEN $a $b)
	;
	
case_else
	: ELSE WS expression
	-> ^(T_CASE_ELSE expression)
	;

COMMENT_LINE
	//: '--' .* ('\r' '\n' | '\r' | '\n') { $channel=HIDDEN; }
	: '--' .* (('\r' '\n') | '\r' | '\n') { Skip(); }
	;
	
//COMMENT_BLOCK
////	: '/*' .* '*/' { $channel=HIDDEN; }
//	: '/*' .* '*/' WS { Skip(); }
//	;

COMMENT_BLOCK
	: '/*' .* '*/' WS { Skip(); }
	;

STRING
	: '\'' ( (~('\'')|'\'''\'')* ) '\''
	;

SIMPLE_FILE
	: '[' ~('@'|'\''|'('|')'|'['|']')* ']'
	;


SELECT 	: S E L E C T ;
ALL	: A L L;
DISTINCT
	: D I S T I N C T;
TOP	: T O P;
BOTTOM	: B O T T O M;
INTO	: I N T O;
FROM 	: F R O M;
WHERE 	: W H E R E;

NOT 	: N O T;
AND 	: A N D;
OR 	: O R;
LIKE	: L I K E;
MATCH	: M A T C H;
GROUP	: G R O U P;
ORDER	: O R D E R;
BY	: B Y;
ASC	: A S C;
DESC	: D E S C;
ORIG	: O R I G;
CONVERT : C O N V E R T;
CAST 	: C A S T;
AS      : A S;
BETWEEN	: B E T W E E N;
IN	: I N;
ANY	: A N Y;
SOME	: S O M E;
EXISTS 	: E X I S T S;
CASE 	: C A S E;
WHEN	: W H E N;
THEN	: T H E N;
ELSE	: E L S E;
END	: E N D;
USE     : U S E;
DECLARE	: D E C L A R E;
SET     : S E T;
CREATE	: C R E A T E;
VIEW	: V I E W;
TABLE	: T A B L E;
DROP	: D R O P;
HAVING	: H A V I N G;
COUNT 	: C O U N T;
DATEADD : D A T E A D D;
DATEDIFF: D A T E D I F F;
DATEPART: D A T E P A R T;
UNION 	: U N I O N;
SIMPLEPIVOT   
	: S I M P L E P I V O T;
FOR	: F O R;
WITH	: W I T H;

TOKEN
	: ('A'..'Z' | 'a'..'z' | '_') ('A'..'Z' | 'a'..'z' | '_' | '0'..'9')*
	;

SYSTEMVAR
	: '$' TOKEN
	;
	
VARIABLE
	: '@' TOKEN
	;

NUMBER
	: DIGIT+ ('k' | 'M' | 'G' | 'T' | 'P' | 'E')?
	;

WS
	: (' '|'\t'|'\n'|'\r'|'\u000C')+
	// ('--' ~('\n' | '\r')* ('\n' | '\r')+)*
//	| '/*' (options {greedy=false;} : .)* '*/' {$channel=HIDDEN;}
	;
    
fragment DIGIT	:	'0'..'9';

fragment A:('a'|'A');
fragment B:('b'|'B');
fragment C:('c'|'C');
fragment D:('d'|'D');
fragment E:('e'|'E');
fragment F:('f'|'F');
fragment G:('g'|'G');
fragment H:('h'|'H');
fragment I:('i'|'I');
fragment J:('j'|'J');
fragment K:('k'|'K');
fragment L:('l'|'L');
fragment M:('m'|'M');
fragment N:('n'|'N');
fragment O:('o'|'O');
fragment P:('p'|'P');
fragment Q:('q'|'Q');
fragment R:('r'|'R');
fragment S:('s'|'S');
fragment T:('t'|'T');
fragment U:('u'|'U');
fragment V:('v'|'V');
fragment W:('w'|'W');
fragment X:('x'|'X');
fragment Y:('y'|'Y');
fragment Z:('z'|'Z');
