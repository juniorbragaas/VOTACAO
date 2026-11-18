import jsPDF from 'jspdf';
import autoTable from 'jspdf-autotable';

function removerCaracteresInvalidos(valor: string): string {
  const limpo = valor.replace(/[\\/:*?"<>|]/g, '_').trim();
  return limpo || 'resultado';
}

export function exportarTabelaPdf(
  columns: string[],
  rows: Record<string, unknown>[],
  titulo: string
): void {
  const orientacao = columns.length > 5 ? 'landscape' : 'portrait';
  const doc = new jsPDF({ orientation: orientacao, unit: 'pt', format: 'a4' });

  const tituloFinal = titulo.trim() || 'Relatório de Resultados';
  const agora = new Date().toLocaleString('pt-BR');

  doc.setFont('helvetica', 'bold');
  doc.setFontSize(15);
  doc.setTextColor(27, 54, 93);
  doc.text(tituloFinal, 40, 42);

  doc.setFont('helvetica', 'normal');
  doc.setFontSize(9);
  doc.setTextColor(100, 116, 139);
  doc.text(`AVALIAÇÃO DE MODELOS  •  Gerado em ${agora}  •  ${rows.length} registro(s)`, 40, 60);

  const body = rows.map((row) =>
    columns.map((col) => {
      const val = row[col];
      return val === null || val === undefined ? '' : String(val);
    })
  );

  autoTable(doc, {
    startY: 74,
    head: [columns],
    body,
    theme: 'grid',
    headStyles: {
      fillColor: [30, 41, 59],
      textColor: [255, 255, 255],
      fontStyle: 'bold',
      fontSize: 9,
    },
    styles: {
      fontSize: 9,
      cellPadding: 6,
      textColor: [30, 41, 59],
    },
    alternateRowStyles: {
      fillColor: [248, 250, 252],
    },
    didDrawPage: (data) => {
      const pageCount = doc.getNumberOfPages();
      const pageSize = doc.internal.pageSize;
      doc.setFontSize(8);
      doc.setTextColor(148, 163, 184);
      doc.text(
        `Página ${data.pageNumber} de ${pageCount}`,
        pageSize.getWidth() - 95,
        pageSize.getHeight() - 20
      );
    },
  });

  doc.save(`${removerCaracteresInvalidos(tituloFinal)}.pdf`);
}
