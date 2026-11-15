from reportlab.pdfgen import canvas
from reportlab.lib import colors
from reportlab.lib.styles import ParagraphStyle
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, PageBreak, KeepTogether
from reportlab.lib.enums import TA_CENTER, TA_JUSTIFY
from pathlib import Path
import pypdfium2 as pdfium

out=Path('output/pdf/Capitulo_6_Luilton_Pichardo_Hernandez.pdf')
navy=colors.HexColor('#18324B')
body=ParagraphStyle('body',fontName='Helvetica',fontSize=11,leading=16,alignment=TA_JUSTIFY,spaceAfter=10)
question=ParagraphStyle('question',parent=body,fontName='Helvetica-Bold',textColor=navy,alignment=0,spaceBefore=12,spaceAfter=8)
center=ParagraphStyle('center',parent=body,alignment=TA_CENTER,leading=19)
title=ParagraphStyle('title',parent=center,fontName='Helvetica-Bold',fontSize=23,leading=30,textColor=navy)
small=ParagraphStyle('small',parent=body,fontSize=9,leading=13,alignment=0)
story=[]
def p(t,style=body): story.append(Paragraph(t,style))
def qa(n,q,a):
    story.append(KeepTogether([Paragraph(f'{n}. {q}',question),Paragraph(a,body)]))
def page(): story.append(PageBreak())
p('UNIVERSIDAD AUTÓNOMA DE SANTO DOMINGO',ParagraphStyle('u',parent=center,fontName='Helvetica-Bold',fontSize=14))
p('RECINTO SAN FRANCISCO DE MACORÍS',center)
story.append(Spacer(1,65))
p('SISTEMAS OPERATIVOS',title)
p('INF324',center)
story.append(Spacer(1,28))
p('Preguntas del capítulo 6',title)
p('Interbloqueos',center)
story.append(Spacer(1,65))
p('<b>Presentado por</b><br/>Luilton Pichardo Hernandez',center)
p('<b>Matrícula</b><br/>100731976',center)
story.append(Spacer(1,30))
p('<b>Docente</b><br/>Ing. Nelson Abreu',center)
p('14 de noviembre de 2026',center)
page()
p('Capítulo 6 | Conceptos fundamentales',title)
qa(1,'¿Qué son interbloqueos?',
'Son situaciones en las que un conjunto de procesos queda bloqueado permanentemente porque cada uno espera un recurso o un evento que depende de otro proceso del mismo conjunto. Ninguno puede continuar y liberar lo que los demás necesitan. Por ejemplo, un proceso conserva el recurso A y solicita B, mientras otro conserva B y solicita A.')
qa(2,'¿Qué son recursos reutilizables?',
'Son recursos que pueden ser utilizados por un proceso y, después de liberarse, quedar disponibles para otros sin agotarse por el uso. Cuando requieren acceso exclusivo, solo un proceso puede utilizarlos a la vez. Ejemplos: una impresora, una región de memoria asignada, un dispositivo de almacenamiento y un mutex. El uso modifica su estado, pero no elimina el recurso.')
qa(3,'¿Qué son recursos consumibles?',
'Son recursos que un proceso produce y que dejan de estar disponibles una vez que otro proceso los consume. Su cantidad puede variar durante la ejecución. Ejemplos: mensajes en una cola, señales, notificaciones de eventos y datos pendientes de recibir en un búfer. Para disponer de una nueva unidad, debe producirse otro mensaje, señal o dato.')
qa(4,'¿Cuáles entidades se pueden distinguir en un sistema?',
'Para analizar los interbloqueos se distinguen principalmente los procesos y los recursos. Los procesos solicitan, utilizan y liberan recursos; los recursos son los elementos necesarios para ejecutar sus tareas y pueden tener una o varias instancias. En un grafo de asignación, ambos se representan como nodos: una arista del proceso al recurso indica una solicitud y una del recurso al proceso indica una asignación.')
page()
p('Condiciones y estrategias',title)
qa(5,'¿Cuáles condiciones deben darse para producirse un interbloqueo?',
'Deben concurrir las cuatro condiciones de Coffman:<br/><b>Exclusión mutua:</b> al menos un recurso requiere uso exclusivo.<br/><b>Retención y espera:</b> un proceso conserva recursos mientras espera otros.<br/><b>No expropiación:</b> los recursos no pueden retirarse por la fuerza; se liberan voluntariamente.<br/><b>Espera circular:</b> existe una cadena cerrada de procesos en la que cada uno espera un recurso retenido por el siguiente. Romper alguna de estas condiciones impide el interbloqueo del modelo considerado.')
qa(6,'¿Cómo se clasifican las técnicas existentes para tratar interbloqueos?',
'Se clasifican en prevención, evitación y detección con recuperación. La prevención impone restricciones que eliminan alguna condición necesaria; la evitación evalúa las asignaciones antes de concederlas; y la detección con recuperación permite que el problema aparezca para identificarlo y resolverlo. También existe la política de ignorar el problema cuando el costo de controlarlo supera el perjuicio esperado.')
qa(7,'Describa las estrategias a tomar para tratar los interbloqueos.',
'<b>Prevención:</b> definir reglas, como solicitar todos los recursos al inicio o respetar un orden global de adquisición.<br/><b>Evitación:</b> conceder una solicitud solo cuando la asignación conserva un estado seguro; suele requerir conocer las necesidades máximas de los procesos.<br/><b>Detección y recuperación:</b> revisar las dependencias y, si existe un interbloqueo, terminar procesos o recuperar recursos.<br/><b>Ignorar:</b> no implantar un tratamiento general y recurrir a una intervención cuando el bloqueo se manifieste. La elección depende del costo, la frecuencia y la importancia de los procesos.')
page()
p('Prevención y detección',title)
qa(8,'¿Cuáles son los mecanismos de prevención de interbloqueos?',
'Los mecanismos buscan negar una condición necesaria:<br/><b>Reducir la exclusión mutua:</b> compartir los recursos que lo permitan; no resulta aplicable a todos los dispositivos o datos.<br/><b>Eliminar la retención y espera:</b> pedir todos los recursos juntos o liberar los retenidos antes de solicitar otros.<br/><b>Permitir expropiación:</b> retirar recursos cuando sea posible guardar y restaurar su estado de forma correcta.<br/><b>Eliminar la espera circular:</b> numerar los tipos de recursos y exigir que se soliciten en orden creciente. Estas restricciones pueden reducir el aprovechamiento de los recursos o causar esperas prolongadas.')
qa(9,'Indique los algoritmos utilizados para la prevención de interbloqueos.',
'En sentido estricto, la prevención utiliza protocolos de solicitud conjunta, liberación antes de nuevas solicitudes, expropiación y ordenación jerárquica de recursos. Si todos los procesos adquieren los bloqueos en el mismo orden global, no puede formarse una espera circular.<br/>Conviene distinguirlos de los algoritmos de <b>evitación</b>: el <b>algoritmo del banquero</b>, que comprueba la seguridad de cada asignación con varias instancias por tipo de recurso, y el <b>algoritmo del grafo de asignación con aristas de reclamación</b>, aplicable a una instancia por tipo. Estos últimos evitan estados inseguros; no eliminan de manera estructural una condición de Coffman.')
qa(10,'¿Cuáles son los algoritmos para detectar interbloqueos?',
'<b>Una instancia por tipo de recurso:</b> se utiliza un grafo de espera entre procesos y se buscan ciclos, por ejemplo mediante un recorrido en profundidad. Un ciclo indica interbloqueo.<br/><b>Varias instancias por tipo:</b> se emplea un algoritmo matricial con los recursos disponibles, los asignados y las solicitudes pendientes. Se simula la terminación de procesos cuyas solicitudes pueden atenderse y la devolución de sus recursos. Los procesos que conservan recursos y no pueden eliminarse de esta simulación integran el interbloqueo. Con varias instancias, un ciclo por sí solo no basta para demostrarlo.')
page()
p('Recuperación y estado seguro',title)
qa(11,'¿En qué consiste el proceso de recuperación del interbloqueo?',
'Consiste en romper las dependencias que mantienen bloqueados a los procesos y restablecer el avance del sistema. Puede hacerse terminando todos los procesos afectados o uno por uno hasta eliminar el ciclo. Otra opción es expropiar recursos recuperables y devolver al proceso víctima a un punto de control para reiniciarlo. La selección de la víctima considera su prioridad, el trabajo realizado, los recursos retenidos y el costo de reinicio. Debe evitarse elegir siempre al mismo proceso, porque podría sufrir inanición.')
qa(12,'¿En qué consiste el estado seguro?',
'Es un estado de asignación en el que existe al menos un orden de ejecución, llamado secuencia segura, que permite a todos los procesos obtener sus recursos restantes, terminar y devolver los recursos, sin superar sus demandas máximas declaradas.<br/>Por ejemplo, hay tres unidades de un recurso: P1 tiene una y necesita como máximo otra; P2 tiene una y necesita como máximo dos adicionales; queda una libre. Puede terminar primero P1 y devolver sus dos unidades. Después P2 obtiene las dos que necesita y termina. La secuencia P1, P2 es segura. Un estado inseguro no implica necesariamente que ya exista un interbloqueo; significa que no se puede garantizar su ausencia.')
qa(13,'¿Cuál es la diferencia entre recursos internos del sistema y recursos de usuario?',
'La diferencia se refiere a su función y a quién los gestiona directamente. Los <b>recursos internos del sistema</b> sostienen el funcionamiento del sistema operativo: estructuras del núcleo, bloqueos internos, tablas de procesos y búferes del sistema. Los <b>recursos de usuario</b> se vinculan con las tareas de las aplicaciones: archivos de trabajo, registros de una base de datos, objetos compartidos y bloqueos definidos por el programa. El sistema operativo puede administrar el acceso a ambos; llamar a un recurso «de usuario» no significa que quede fuera de su control. Pueden existir interbloqueos en cualquiera de los dos ámbitos.')
p('Referencias de consulta',question)
p('Cuestionario base: <i>Preguntas capítulo 6</i>, INF324, UASD, preparado por Ing. Nelson Abreu. Respuestas redactadas para este material.',small)
p('John Bell, University of Illinois Chicago. <i>Operating Systems: Deadlocks</i>. <link href="https://www.cs.uic.edu/~jbell/CourseNotes/OperatingSystems/7_Deadlocks.html" color="#18324B">Apuntes sobre interbloqueos</link>. Consulta de apoyo para clasificación y terminología.',small)
p('Allan Gottlieb, New York University. <i>Operating Systems, Lecture 12</i>. <link href="https://cs.nyu.edu/~gottlieb/courses/2000s/2003-04-spring/os202/lectures/lecture-12.html" color="#18324B">Estado seguro y algoritmo del banquero</link>.',small)
def footer(c,d):
    if d.page>1:
        c.setStrokeColor(colors.HexColor('#D1D9DF')); c.line(48,42,564,42)
        c.setFont('Helvetica',8); c.setFillColor(navy)
        c.drawString(48,29,'Luilton Pichardo Hernandez | 100731976 | INF324')
        c.drawRightString(564,29,str(d.page))
doc=SimpleDocTemplate(str(out),pagesize=(612,792),rightMargin=48,leftMargin=48,topMargin=48,bottomMargin=58,title='Preguntas del capítulo 6 - Interbloqueos',author='Luilton Pichardo Hernandez')
doc.build(story,onFirstPage=footer,onLaterPages=footer)
d=pdfium.PdfDocument(str(out))
from PIL import Image,ImageOps,ImageDraw
thumbs=[]
for i in range(len(d)):
    img=d[i].render(scale=1.25).to_pil().convert('RGB'); img.save(f'tmp/pdfs/pagina_{i+1}.png')
    img.thumbnail((459,594)); thumbs.append(img)
sheet=Image.new('RGB',(459*3,620*2),'#d8d8d8')
for i,img in enumerate(thumbs): sheet.paste(img,((i%3)*459,(i//3)*620))
sheet.save('tmp/pdfs/revision.png')
print(f'{out.resolve()} | {len(d)} páginas')
