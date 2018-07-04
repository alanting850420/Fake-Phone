using System;

namespace BeanFunRegPop
{
    internal class ErrorInformationDialog
    {
        private Exception exception;

        public ErrorInformationDialog(Exception exception)
        {
            this.exception = exception;
        }
    }
}